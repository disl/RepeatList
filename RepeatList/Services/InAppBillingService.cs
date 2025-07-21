using Plugin.InAppBilling;

namespace RepeatList.Services
{
   

    public class InAppBillingService
    {
        private const string ProductId = "premium_monthly_299";
        private const string HasPremiumKey = "HasPremium";
        private const string PremiumPurchaseIdKey = "PremiumAboID";

        public async Task<bool> PayPremiumMonthAsync()
        {
            try
            {
                var billing = CrossInAppBilling.Current;

                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                var purchase = await billing.PurchaseAsync(ProductId, ItemType.Subscription, "de");

                if (purchase == null)
                    return false;

                if (purchase.State == PurchaseState.Purchased)
                {
                    // Kauf erfolgreich
                    Preferences.Set(HasPremiumKey, true);
                    Preferences.Set(PremiumPurchaseIdKey, purchase.Id);
                    return true;
                }

                return false;
            }
            catch (InAppBillingPurchaseException ex)
            {
                // Fehlerbehandlung (z. B. Benutzer bricht ab)
                Console.WriteLine("Fehler beim Kauf: " + ex.Message);
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public bool IsPremiumActive()
        {
            return Preferences.Get(HasPremiumKey, false);
        }

        public async Task<bool> CheckAktivesAboAsync()
        {
            var billing = CrossInAppBilling.Current;

            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                var purchases = await billing.GetPurchasesAsync(ItemType.Subscription);

                var premium = purchases?.FirstOrDefault(p => p.ProductId == ProductId &&
                                                             p.AutoRenewing &&
                                                             p.State == PurchaseState.Purchased);

                bool aktiv = premium != null;
                Preferences.Set(HasPremiumKey, aktiv);
                return aktiv;
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
    }

}
