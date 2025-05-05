#if ANDROID
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.Install.Model;
using Xamarin.Google.Android.Play.Core.Tasks;

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
                if (appUpdateInfo.UpdateAvailability() == UpdateAvailability.UpdateAvailable)
                {
                    // Check if the update is allowed
                    // For immediate updates:
                    if (appUpdateInfo.IsUpdateTypeAllowed(AppUpdateType.Immediate))
                    {
                        _updateManager.StartUpdateFlowForResult(
                            appUpdateInfo,
                            AppUpdateType.Immediate,
                            Platform.CurrentActivity ?? throw new NullReferenceException("CurrentActivity is null"),
                            UpdateRequestCode);
                    }
                    // For flexible updates:
                    else if (appUpdateInfo.IsUpdateTypeAllowed(AppUpdateType.Flexible))
                    {
                        _updateManager.StartUpdateFlowForResult(
                            appUpdateInfo,
                            AppUpdateType.Flexible,
                            Platform.CurrentActivity ?? throw new NullReferenceException("CurrentActivity is null"),
                            UpdateRequestCode);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex != null)
                    SentrySdk.CaptureException(ex);

                // Handle or log the exception
                Console.WriteLine($"InAppUpdate failed: {ex.Message}");
                throw;
            }
        }
    }

    public class PlayCoreTaskWrapper<T> : Java.Lang.Object, IOnSuccessListener, IOnFailureListener where T : class
    {
        private readonly TaskCompletionSource<T> _tcs = new();

        public PlayCoreTaskWrapper(Xamarin.Google.Android.Play.Core.Tasks.Task task)
        {
            task.AddOnSuccessListener(this);
            task.AddOnFailureListener(this);
        }

        public Task<T> GetAsync() => _tcs.Task;

        public void OnSuccess(Java.Lang.Object result)
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