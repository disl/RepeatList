using Plugin.InAppBilling;

namespace RepeatList.Services
{
    public class InAppBillingService
    {
        private const string SubscriptionProductId = "premium_monthly_399";
        private const string In_app_1000_prompts = "in_app_1000_prompts";

        private const string PremiumKey = "HasPremiumSubscription";
        private const string TokenKey = "QueryTokenCount";
        private const string LastQueryDateKey = "QueryDate";
        private const string DailyQueryCountKey = "QueriesToday";
        private const int FreeDailyLimit = 5;
        private const int TokenPackAmount = 1000;

        public async Task<bool> PurchaseSubscriptionAsync()
        {
            return await PurchaseProductAsync(SubscriptionProductId, ItemType.Subscription);
        }

        public async Task<bool> PurchaseTokenPackAsync()
        {
            bool success = await PurchaseProductAsync(In_app_1000_prompts, ItemType.InAppPurchase);
            if (success)
            {
                int current = Preferences.Get(TokenKey, 0);
                Preferences.Set(TokenKey, current + TokenPackAmount);
            }
            return success;
        }

        private async Task<bool> PurchaseProductAsync(string productId, ItemType type)
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();
                if (!connected) return false;

                var purchase = await billing.PurchaseAsync(productId, type, "en");

                if (purchase?.State == PurchaseState.Purchased)
                {
                    if (productId == SubscriptionProductId)
                        Preferences.Set(PremiumKey, true);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            var billing = CrossInAppBilling.Current;

            try
            {
                await billing.ConnectAsync();

                var subscriptions = await billing.GetPurchasesAsync(ItemType.Subscription);
                bool isActive = subscriptions?.Any(p => p.ProductId == SubscriptionProductId &&
                                                        p.State == PurchaseState.Purchased &&
                                                        p.AutoRenewing) == true;
                Preferences.Set(PremiumKey, isActive);
                return isActive;
            }
            catch
            {
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        public bool HasActiveSubscription() => Preferences.Get(PremiumKey, false);

        public int GetAvailableTokens() => Preferences.Get(TokenKey, 0);

        public void ConsumeToken()
        {
            int tokens = GetAvailableTokens();
            if (tokens > 0)
                Preferences.Set(TokenKey, tokens - 1);
        }

        public bool IsFreeLimitReached()
        {
            DateTime today = DateTime.Today;
            string storedDate = Preferences.Get(LastQueryDateKey, "");
            int queriesToday = Preferences.Get(DailyQueryCountKey, 0);

            if (DateTime.TryParse(storedDate, out var lastDate))
            {
                if (lastDate.Date != today)
                {
                    Preferences.Set(DailyQueryCountKey, 0);
                    Preferences.Set(LastQueryDateKey, today.ToString("yyyy-MM-dd"));
                    return false;
                }
            }
            else
            {
                Preferences.Set(LastQueryDateKey, today.ToString("yyyy-MM-dd"));
                Preferences.Set(DailyQueryCountKey, 0);
                return false;
            }

            return queriesToday >= FreeDailyLimit;
        }

        public void IncrementFreeUsage()
        {
            int queriesToday = Preferences.Get(DailyQueryCountKey, 0);
            Preferences.Set(DailyQueryCountKey, queriesToday + 1);
        }

        public async Task<bool> CanExecuteQueryAsync(bool IsIncrementFrreUsage)
        {
            if (HasActiveSubscription())
                return true;

            if (GetAvailableTokens() > 0)
            {
                ConsumeToken();
                return true;
            }

            //if (!IsFreeLimitReached())
            //{
            //    if (IsIncrementFrreUsage)
            //        IncrementFreeUsage();
            //    return true;
            //}

            return false; // Blocked: no tokens and free limit reached
        }
    }
}
