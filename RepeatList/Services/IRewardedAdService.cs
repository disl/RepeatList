namespace RepeatList.Services
{
    public interface IRewardedAdService
    {
        Task<bool> ShowRewardedAdAsync();
        bool IsAdReady { get; }
    }
}
