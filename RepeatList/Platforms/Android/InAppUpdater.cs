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
                    // Check if the update is allowed
                    // For immediate updates:
                    if (appUpdateInfo.IsUpdateTypeAllowed(IAppUpdateType.Immediate))
                    {
                        var options = AppUpdateOptions.DefaultOptions(IAppUpdateType.Immediate);
                        _updateManager.StartUpdateFlowForResult(
                            appUpdateInfo,
                            Platform.CurrentActivity ?? throw new NullReferenceException("CurrentActivity is null"),
                            options,
                            UpdateRequestCode);
                    }
                    // For flexible updates:
                    else if (appUpdateInfo.IsUpdateTypeAllowed(IAppUpdateType.Flexible))
                    {
                        var options = AppUpdateOptions.DefaultOptions(IAppUpdateType.Flexible);
                        _updateManager.StartUpdateFlowForResult(
                            appUpdateInfo,
                            Platform.CurrentActivity ?? throw new NullReferenceException("CurrentActivity is null"),
                            options,
                            UpdateRequestCode);
                    }
                }
            }
            catch (Exception ex)
            {
                // ERROR_APP_NOT_OWNED (-10) tritt bei Debug-/Sideload-Installs auf (App wurde
                // nicht über den Play Store erworben). Das ist ein erwarteter, harmloser Zustand
                // und kein Fehler im eigentlichen Sinn → nicht an Sentry melden.
                if (ex.Message.Contains("-10", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("not owned", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("app not owned", StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"InAppUpdater skipped (expected for sideload): {ex.Message}");
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
