#if ANDROID
using Android.Gms.Tasks;
using Com.Google.Android.Play.Core.Appupdate;
using Com.Google.Android.Play.Core.Install.Model;
using Sentry;

namespace RepeatList.Platforms.Android
{
    public class InAppUpdater
    {
        private const int UpdateRequestCode = 123;
        private IAppUpdateManager _updateManager;

        public async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            try
            {
                var context = Platform.AppContext;
                if (context == null)
                {
                    throw new InvalidOperationException("Android context is not available");
                }

                _updateManager = AppUpdateManagerFactory.Create(context);

                // Get the update info task
                var appUpdateInfoTask = _updateManager.AppUpdateInfo;

                // Wait for the task to complete
                var appUpdateInfo = await new PlayCoreTaskWrapper<AppUpdateInfo>(appUpdateInfoTask).GetAsync();

                // Check if update is available
                if (appUpdateInfo.UpdateAvailability() == IUpdateAvailability.UpdateAvailable)
                {
                    // Update-Typ bestimmen (Immediate bevorzugt, sonst Flexible)
                    int? updateType = null;
                    if (appUpdateInfo.IsUpdateTypeAllowed(IAppUpdateType.Immediate))
                    {
                        updateType = IAppUpdateType.Immediate;
                    }
                    else if (appUpdateInfo.IsUpdateTypeAllowed(IAppUpdateType.Flexible))
                    {
                        updateType = IAppUpdateType.Flexible;
                    }

                    if (updateType.HasValue)
                    {
                        var activity = Platform.CurrentActivity;

                        // Timer-getriggerter Aufruf: Activity kann gerade terminieren
                        // (Hintergrund/Rotation/Beenden). StartUpdateFlowForResult braucht
                        // eine gültige, aktive Activity - sonst wirft Play Core intern eine
                        // Java.Lang.Exception aus der JNI-Schicht. Beim nächsten Timer-Tick
                        // wird es erneut versucht, daher hier einfach überspringen.
                        if (activity == null || activity.IsFinishing || activity.IsDestroyed)
                        {
                            return;
                        }

                        var options = AppUpdateOptions.DefaultOptions(updateType.Value);
                        _updateManager.StartUpdateFlowForResult(
                            appUpdateInfo,
                            activity,
                            options,
                            UpdateRequestCode);
                    }
                }
            }
            catch (Exception ex)
            {
                // Harmlose, erwartete Play-Core-Zustände nicht an Sentry melden:
                //  - ERROR_APP_NOT_OWNED (-10) / "not owned": Debug-/Sideload-Install (App wurde
                //    nicht über den Play Store erworben).
                //  - "not found": Update-Info-Abfrage ohne Ergebnis.
                //  - "Binder has died" / "DeadObject": Der Play-Store-Prozess (Binder/IPC-Partner)
                //    wurde beendet oder neu gestartet, während wir AppUpdateInfo abfragten. Das ist
                //    transient und behebt sich beim nächsten Update-Check von selbst — kein App-Bug.
                string msg = ex.GetBaseException().Message ?? string.Empty;
                if (msg.Contains("-10", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("not owned", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("app not owned", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("binder has died", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("DeadObject", StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"InAppUpdater skipped (expected): {msg}");
#endif
                    return;
                }

                SentrySdk.CaptureException(ex);
            }
        }
    }

    public class PlayCoreTaskWrapper<T> : Java.Lang.Object, IOnSuccessListener, IOnFailureListener where T : class
    {
        private readonly System.Threading.Tasks.TaskCompletionSource<T> _tcs = new();

        public PlayCoreTaskWrapper(global::Android.Gms.Tasks.Task task)
        {
            task.AddOnSuccessListener(this);
            task.AddOnFailureListener(this);
        }

        public System.Threading.Tasks.Task<T> GetAsync() => _tcs.Task;

        public void OnSuccess(Java.Lang.Object? result)
        {
            if (result is T typedResult)
            {
                _tcs.TrySetResult(typedResult);
            }
            else
            {
                _tcs.TrySetException(new InvalidCastException($"Cannot cast {result?.GetType().Name} to {typeof(T).Name}"));
            }
        }

        public void OnFailure(Java.Lang.Exception e)
        {
            _tcs.TrySetException(new Exception(e?.Message ?? "Unknown error in Play Core task"));
        }
    }
}
#endif
