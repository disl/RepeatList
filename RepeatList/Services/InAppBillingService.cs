using Microsoft.Maui.Storage;
using Plugin.InAppBilling;
using RepeatList.Models;
using System.Globalization;

namespace RepeatList.Services
{
    public class InAppBillingService
    {
        private const string SubscriptionProductId = "premium_monthly_399";
        private const string In_app_1000_prompts = "in_app_1000_prompts";

        private const string PremiumKey = "HasPremiumSubscription";
        private const string LastQueryDateKey = "QueryDate";
        private const string DailyQueryCountKey = "QueriesToday";
        private const string AdBonusKey = "AdBonusQueries";
        public const int FreeDailyLimit = 1;
        public const decimal TokenPackAmount_199 = 1.99m;

        public async Task<bool> PurchaseSubscriptionAsync()
        {
            return await PurchaseProductAsync(SubscriptionProductId, ItemType.Subscription);
        }

        public async Task<bool> PurchaseTokenPackAsync()
        {
            bool success = await PurchaseProductAsync(In_app_1000_prompts, ItemType.InAppPurchase);
            if (success)
            {
                decimal current = Convert.ToDecimal(Preferences.Get(DeepSeekBilling.Preferences_key__user_credit, "0"));
                var newCredit = current + TokenPackAmount_199;
                Preferences.Set(DeepSeekBilling.Preferences_key__user_credit, newCredit.ToString(CultureInfo.GetCultureInfo("en-US")));
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

                InAppBillingPurchase purchase = await billing.PurchaseAsync(productId, type, "en");

                if (purchase?.State == PurchaseState.Purchased)
                {
                    if (productId == SubscriptionProductId)
                    {
                        Preferences.Set(PremiumKey, true);
                        await SyncPurchaseToSupabaseAsync(purchase.PurchaseToken, productId);
                    }
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        private async Task SyncPurchaseToSupabaseAsync(string? purchaseToken, string productId)
        {
            var deviceId = GetDeviceId();
            if (deviceId == null) return;

            var supabase = SupabaseService.Shared;
            await supabase.UpsertSubscriptionAsync(deviceId, true, purchaseToken, productId);
        }

        public async Task SyncFromSupabaseAsync()
        {
            var deviceId = GetDeviceId();
            if (deviceId == null) return;

            try
            {
                var supabase = SupabaseService.Shared;
                var isPremium = await supabase.GetSubscriptionStatusAsync(deviceId);
                if (isPremium.HasValue)
                    Preferences.Set(PremiumKey, isPremium.Value);
            }
            catch (Exception)
            {
                // Offline — lokaler Cache bleibt
            }
        }

        private static string? GetDeviceId()
        {
#if ANDROID
            return Android.Provider.Settings.Secure.GetString(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId);
#else
            return null;
#endif
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

        public decimal GetAvailableTokens()
        {
            var tokenString = Preferences.Get(DeepSeekBilling.Preferences_key__user_credit, "0");
            return Convert.ToDecimal(tokenString, new CultureInfo("en-US"));
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

        public async Task<bool> CanExecuteQueryAsync()
        {
            if (HasActiveSubscription())
                return true;

            if (GetAvailableTokens() > 0)
                return true;

            return false;
        }

        public void AddAdBonus(int queries = 5) =>
            Preferences.Set(AdBonusKey, Preferences.Get(AdBonusKey, 0) + queries);

        public async Task<bool> CanExecuteQueryForFreeAsync(bool IsDecrementAktive)
        {
            int adBonus = Preferences.Get(AdBonusKey, 0);
            if (adBonus > 0)
            {
                if (IsDecrementAktive)
                    Preferences.Set(AdBonusKey, adBonus - 1);
                return true;
            }

            if (!IsFreeLimitReached())
            {
                if (IsDecrementAktive)
                    IncrementFreeUsage();
                return true;
            }

            return false;
        }
    }
}
