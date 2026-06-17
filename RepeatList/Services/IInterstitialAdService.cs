namespace RepeatList.Services
{
    public interface IInterstitialAdService
    {
        void LoadAd();
        Task ShowAdIfReadyAsync();
        bool IsAdReady { get; }
    }
}
