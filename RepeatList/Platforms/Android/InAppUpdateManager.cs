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
    // Bei jedem App-Resume wird geprüft: Immediate-Update bevorzugt (Blocking-Screen, stärkste
    // UX), sonst Flexible (lädt im Hintergrund, fragt nach Abschluss zum Neustart). Läuft über
    // AddOnSuccessListener statt eines InstallStateUpdatedListener - dessen generische
    // Java-Signatur lässt sich mit den aktuellen Play-Core-Bindings nicht sauber implementieren
    // (javac-Fehler "onStateUpdate(Object)" vs. "onStateUpdate(InstallState)" bei Erasure-Konflikt).
    internal static class InAppUpdateManager
    {
        const int UpdateRequestCode = 4711;

        static IAppUpdateManager? _manager;

        // Von MainActivity.OnResume/OnPause gesetzt; true, solange die Activity im Vordergrund ist.
        public static volatile bool IsForeground;

        public static void CheckForUpdate(Activity activity)
        {
            // Play Core auf Geräten ohne Play Store (Sideload/Debug-Install) gar nicht erst
            // anfragen - AppUpdateManagerFactory.Create bzw. der spätere Callback würden sonst
            // mit "not owned"/"not found" durchlaufen, was hier nur unnötig Arbeit macht.
            if (!IsPlayStoreAvailable(activity))
                return;

            _manager ??= AppUpdateManagerFactory.Create(activity);
            var manager = _manager;

            manager.AppUpdateInfo.AddOnSuccessListener(new AppUpdateInfoListener(info =>
                OnAppUpdateInfo(manager, activity, info)));
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
            if (info.UpdateAvailability() == IUpdateAvailability.UpdateAvailable)
            {
                // Update-Typ bestimmen (Immediate bevorzugt, sonst Flexible).
                int? updateType = null;
                if (info.IsUpdateTypeAllowed(IAppUpdateType.Immediate))
                    updateType = IAppUpdateType.Immediate;
                else if (info.IsUpdateTypeAllowed(IAppUpdateType.Flexible))
                    updateType = IAppUpdateType.Flexible;

                if (!updateType.HasValue)
                    return;

                // AppUpdateInfo kommt asynchron vom Play-Store-Prozess zurück - zwischen dem
                // CheckForUpdate-Aufruf (OnResume) und diesem Callback kann die Activity
                // pausiert, rotiert oder zerstört worden sein (z. B. schneller App-Wechsel).
                // StartUpdateFlowForResult startet dann einen IntentSender auf einer nicht mehr
                // gültigen Activity → SendIntentException. Beim nächsten OnResume wird es erneut
                // versucht, daher hier einfach überspringen.
                // Zusätzlich muss die Activity noch im Vordergrund (Resumed) sein. Vermutung
                // (unbestätigt) zur generischen "Exception_WasThrown" aus Build 166: Der Flow
                // wurde gestartet, als die Activity nicht mehr aktiv war.
                if (activity.IsFinishing || activity.IsDestroyed || !IsForeground)
                    return;

                try
                {
                    manager.StartUpdateFlowForResult(
                        info, activity,
                        AppUpdateOptions.DefaultOptions(updateType.Value),
                        UpdateRequestCode);
                }
                catch (Java.Lang.Exception ex)
                {
                    // Play Core wirft hier teils eine generische "Exception_WasThrown" aus der
                    // JNI-Schicht, wenn die Activity zwar IsFinishing/IsDestroyed-Checks besteht,
                    // aber intern (z. B. Fenster-Token schon ungültig, App gerade im Hintergrund)
                    // trotzdem nicht mehr aktualisierbar ist. Mit Tag melden statt schlucken, bis
                    // sich zeigt, wie oft das noch vorkommt.
                    SentrySdk.CaptureException(ex, scope => scope.SetTag("in_app_update", "start_flow_failed"));
                }
            }
            else if (info.InstallStatus() == IInstallStatus.Downloaded)
            {
                // Ein Flexible-Update wurde bereits fertig heruntergeladen (in diesem oder
                // einem früheren App-Aufenthalt) - jetzt zum Neustart auffordern.
                PromptRestart(manager);
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
    }
}
