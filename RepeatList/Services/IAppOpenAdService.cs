namespace RepeatList.Services
{
    public interface IAppOpenAdService
    {
        void LoadAd();
        Task ShowAdIfReadyAsync();
        bool IsAdReady { get; }
    }
}
