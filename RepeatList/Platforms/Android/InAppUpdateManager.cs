using Android.App;
using Com.Google.Android.Play.Core.Appupdate;
using Com.Google.Android.Play.Core.Install.Model;
using Sentry;

namespace RepeatList.Platforms.Android
{
    // Google Play In-App-Update: einziger Update-Mechanismus der App (vormals zwei getrennte
    // Implementierungen — InAppUpdater, einmalig bei ListsPage.OnAppearing, und dieser Manager,
    // bei jedem MainActivity.OnResume — wurden hier zusammengeführt, da sich beide auf denselben
    // Play-Core-AppUpdateManager stützten und sich potenziell gegenseitig ins Gehege kamen).
    // Bei jedem App-Resume wird geprüft; angeboten wird nur das Flexible-Update (lädt im
    // Hintergrund, fragt nach Abschluss zum Neustart). Immediate (Vollbild-Update-Zwang) ist
    // bewusst nicht eingebaut, siehe docs/NOTES.md. Läuft über
    // AddOnSuccessListener statt eines InstallStateUpdatedListener - dessen generische
    // Java-Signatur lässt sich mit den aktuellen Play-Core-Bindings nicht sauber implementieren
    // (javac-Fehler "onStateUpdate(Object)" vs. "onStateUpdate(InstallState)" bei Erasure-Konflikt).
    // Alle Felder werden nur vom Main-Thread gelesen/geschrieben (OnResume und die Play-Core-
    // Listener laufen dort), daher ohne Lock.
    internal static class InAppUpdateManager
    {
        const int UpdateRequestCode = 4711;
        const int UpdateType = IAppUpdateType.Flexible;

        // Nach dieser Zeit gilt eine unbeantwortete AppUpdateInfo-Abfrage als verloren und die
        // Sperre wird wieder freigegeben (sonst gäbe es bis zum Prozessende keine Prüfung mehr).
        static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        static IAppUpdateManager? _manager;

        // Startzeit (Environment.TickCount64) der laufenden AppUpdateInfo-Abfrage; 0 = keine.
        static long _requestStartedTicks;

        // true, sobald in diesem Prozess ein Update-Dialog erfolgreich gestartet wurde.
        static bool _flowStarted;

        // Von MainActivity.OnResume/OnPause gesetzt; true, solange die Activity im Vordergrund ist.
        public static volatile bool IsForeground;

        // true, solange Google Play ein Update meldet (verfügbar, im Download oder fertig
        // heruntergeladen). Die Vollbild-Werbung (App-Open, Interstitial) bleibt dann aus, damit
        // sie nicht mit dem Update-Dialog bzw. dem Neustart-Hinweis zusammentrifft.
        public static volatile bool IsUpdatePending;

        // Wird mit der ersten Antwort von Google Play abgeschlossen (auch wenn keine kommen kann).
        static readonly TaskCompletionSource<bool> _firstAnswer =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Für Werbung direkt nach dem App-Start: wartet höchstens "timeout" auf die erste Antwort
        // von Google Play und liefert dann IsUpdatePending. Ohne dieses Warten käme die
        // App-Open-Werbung meist vor der Antwort und läge über dem Update-Dialog.
        public static async Task<bool> IsUpdatePendingAsync(TimeSpan timeout)
        {
            try
            {
                await _firstAnswer.Task.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                // Keine Antwort in der Wartezeit - dann gilt der bisher bekannte Stand.
            }
            return IsUpdatePending;
        }

        public static void CheckForUpdate(Activity activity)
        {
            // Play Core auf Geräten ohne Play Store (Sideload/Debug-Install) gar nicht erst
            // anfragen - AppUpdateManagerFactory.Create bzw. der spätere Callback würden sonst
            // mit "not owned"/"not found" durchlaufen, was hier nur unnötig Arbeit macht.
            if (!IsPlayStoreAvailable(activity))
            {
                _firstAnswer.TrySetResult(false);
                return;
            }

            // Nur eine Abfrage gleichzeitig. OnResume feuert beim Start teils mehrfach kurz
            // hintereinander (Sentry-Breadcrumbs Build 169: Activated → Stopped → Resumed
            // innerhalb von 1,6 s), und jede Abfrage liefert einen eigenen PendingIntent für den
            // Update-Dialog. Build 169 scheiterte in dieser Lage mit
            // IntentSender$SendIntentException (ohne Message = PendingIntent bereits storniert
            // oder verbraucht). Vermutung: Die zweite Abfrage macht den Intent der ersten ungültig.
            long now = Environment.TickCount64;
            if (_requestStartedTicks != 0 && now - _requestStartedTicks < RequestTimeout.TotalMilliseconds)
                return;
            _requestStartedTicks = now;

            _manager ??= AppUpdateManagerFactory.Create(activity);
            var manager = _manager;

            global::Android.Gms.Tasks.Task updateInfoTask = manager.AppUpdateInfo;
            updateInfoTask.AddOnSuccessListener(new AppUpdateInfoListener(info =>
            {
                _requestStartedTicks = 0;
                OnAppUpdateInfo(manager, activity, info);
            }));
            // Fehlgeschlagene Abfragen (z. B. "not owned" bei Sideload) sind erwartbar und
            // werden nicht gemeldet - hier nur die Sperre wieder freigeben.
            updateInfoTask.AddOnFailureListener(new AppUpdateFailureListener(() =>
            {
                _requestStartedTicks = 0;
                _firstAnswer.TrySetResult(IsUpdatePending);
            }));
        }

        static bool IsPlayStoreAvailable(Activity activity)
        {
            try
            {
                var intent = activity.PackageManager?.GetLaunchIntentForPackage("com.android.vending");
                return intent != null;
            }
            catch
            {
                return false;
            }
        }

        static void OnAppUpdateInfo(IAppUpdateManager manager, Activity activity, AppUpdateInfo info)
        {
            int installStatus = info.InstallStatus();
            int availability = info.UpdateAvailability();

            // Vor allen Abbruchbedingungen setzen: Die Werbesperre gilt auch dann, wenn der
            // Update-Dialog in diesem Durchlauf nicht (erneut) gezeigt wird.
            IsUpdatePending = installStatus == IInstallStatus.Downloaded
                || availability == IUpdateAvailability.UpdateAvailable
                || availability == IUpdateAvailability.DeveloperTriggeredUpdateInProgress;
            _firstAnswer.TrySetResult(IsUpdatePending);

            // Zuerst prüfen (wie im Play-Core-Beispiel von Google), unabhängig von
            // UpdateAvailability: Ein Flexible-Update wurde bereits fertig heruntergeladen (in
            // diesem oder einem früheren App-Aufenthalt) - jetzt zum Neustart auffordern.
            if (installStatus == IInstallStatus.Downloaded)
            {
                PromptRestart(manager);
                return;
            }

            if (availability != IUpdateAvailability.UpdateAvailable)
                return;

            // Den Update-Dialog pro Prozess nur einmal starten. Sonst öffnet jedes weitere
            // OnResume ihn erneut - auch direkt nach dem Schließen des Dialogs selbst, denn das
            // löst ebenfalls ein OnResume aus.
            if (_flowStarted)
                return;

            if (!info.IsUpdateTypeAllowed(UpdateType))
                return;

            // AppUpdateInfo kommt asynchron vom Play-Store-Prozess zurück - zwischen dem
            // CheckForUpdate-Aufruf (OnResume) und diesem Callback kann die Activity
            // pausiert, rotiert oder zerstört worden sein (z. B. schneller App-Wechsel).
            // Beim nächsten OnResume wird es erneut versucht, daher hier einfach überspringen.
            if (activity.IsFinishing || activity.IsDestroyed || !IsForeground)
                return;

            try
            {
                _flowStarted = manager.StartUpdateFlowForResult(
                    info, activity,
                    AppUpdateOptions.DefaultOptions(UpdateType),
                    UpdateRequestCode);
            }
            catch (Java.Lang.Exception ex)
            {
                // Bekannt aus Build 162-169: IntentSender$SendIntentException, auch bei gültiger
                // Activity im Vordergrund. Mit Tag melden statt schlucken, um zu sehen, ob die
                // Abfrage-Sperre in CheckForUpdate das beseitigt.
                // Die .NET-Message ist in Release nur ein Ressourcen-Schlüssel; die echte
                // Java-Klasse/-Meldung steckt im Handle und wird deshalb als Kontext mitgeschickt.
                string javaClass = "?", javaMessage = "?", javaCause = "?";
                try { javaClass = global::Android.Runtime.JNIEnv.GetClassNameFromInstance(ex.Handle); } catch { }
                try { javaMessage = ex.LocalizedMessage ?? "(null)"; } catch { }
                try { javaCause = ex.Cause?.ToString() ?? "(null)"; } catch { }

                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("in_app_update", "start_flow_failed");
                    scope.SetTag("java_class", javaClass);
                    scope.SetExtra("java_message", javaMessage);
                    scope.SetExtra("java_cause", javaCause);
                    scope.SetExtra("update_type", UpdateType);
                    scope.SetExtra("is_foreground", IsForeground);
                    scope.SetExtra("android_sdk", (int)global::Android.OS.Build.VERSION.SdkInt);
                });
            }
        }

        static void PromptRestart(IAppUpdateManager manager)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var page = global::Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page == null)
                    return;

                bool restart = await page.DisplayAlert(
                    "Update bereit",
                    "Eine neue Version wurde heruntergeladen. Jetzt neu starten, um sie zu installieren?",
                    "Neu starten", "Später");

                if (restart)
                    manager.CompleteUpdate();
            });
        }

        sealed class AppUpdateInfoListener(Action<AppUpdateInfo> onSuccess)
            : Java.Lang.Object, global::Android.Gms.Tasks.IOnSuccessListener
        {
            public void OnSuccess(Java.Lang.Object result) => onSuccess((AppUpdateInfo)result);
        }

        sealed class AppUpdateFailureListener(Action onFailure)
            : Java.Lang.Object, global::Android.Gms.Tasks.IOnFailureListener
        {
            public void OnFailure(Java.Lang.Exception e) => onFailure();
        }
    }
}
