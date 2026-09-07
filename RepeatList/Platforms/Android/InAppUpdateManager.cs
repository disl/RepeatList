using Android.App;
using Com.Google.Android.Play.Core.Appupdate;
using Com.Google.Android.Play.Core.Install.Model;

namespace RepeatList.Platforms.Android
{
    // Google Play "Flexible" In-App-Update: lädt eine neue Version im Hintergrund
    // herunter, ohne die App zu blockieren, und fragt den Nutzer erst danach,
    // ob er neu starten möchte. Ersetzt das rein passive Warten auf Play-Store-Auto-Update.
    // Der Status wird bei jedem App-Resume abgefragt (statt über einen
    // InstallStateUpdatedListener - dessen generische Java-Signatur lässt sich mit den
    // aktuellen Play-Core-Bindings nicht sauber implementieren, siehe javac-Fehler
    // "onStateUpdate(Object)" vs. "onStateUpdate(InstallState)" bei Erasure-Konflikt).
    internal static class InAppUpdateManager
    {
        const int UpdateRequestCode = 4711;

        static IAppUpdateManager? _manager;

        public static void CheckForUpdate(Activity activity)
        {
            _manager ??= AppUpdateManagerFactory.Create(activity);
            var manager = _manager;

            manager.AppUpdateInfo.AddOnSuccessListener(new AppUpdateInfoListener(info =>
            {
                if (info.UpdateAvailability() == IUpdateAvailability.UpdateAvailable
                    && info.IsUpdateTypeAllowed(IAppUpdateType.Flexible))
                {
                    manager.StartUpdateFlowForResult(
                        info, activity,
                        AppUpdateOptions.NewBuilder(IAppUpdateType.Flexible).Build(),
                        UpdateRequestCode);
                }
                else if (info.InstallStatus() == IInstallStatus.Downloaded)
                {
                    // Update wurde bereits fertig heruntergeladen (in diesem oder einem
                    // früheren App-Aufenthalt) - jetzt zum Neustart auffordern.
                    PromptRestart(manager);
                }
            }));
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
