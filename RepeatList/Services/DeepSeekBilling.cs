using System.Globalization;
using System.Text.Json;

namespace RepeatList.Services
{
    public class DeepSeekBilling
    {
        // Beispielpreise (angepasst auf reale DeepSeek-Preise, sofern bekannt)
        private const decimal PricePerPromptToken = 0.0015m / 1000;      // z. B. $0.0015 / 1000 Tokens
        private const decimal PricePerCompletionToken = 0.0020m / 1000;  // z. B. $0.0020 / 1000 Tokens
        private const decimal PricePer_Correction = 2;
        public static  string Preferences_key__user_credit = "UserCredit";


        /// <summary>
        /// Berechnet die Kosten basierend auf einer DeepSeek-API-Antwort
        /// </summary>
        //public static decimal CalculateCostFromApiResponse(string jsonResponse)
        //{
        //    try
        //    {
        //        using var doc = JsonDocument.Parse(jsonResponse);
        //        var usage = doc.RootElement.GetProperty("usage");

        //        int promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
        //        int completionTokens = usage.GetProperty("completion_tokens").GetInt32();

        //        decimal cost = (promptTokens * PricePerPromptToken) + (completionTokens * PricePerCompletionToken);
        //        return Math.Round(cost, 6); // auf 6 Stellen runden (z. B. 0.003421)
        //    }
        //    catch
        //    {
        //        return 0m; // Fehler – keine Kosten berechnet
        //    }
        //}

        /// <summary>
        /// Zieht Kosten vom Benutzerguthaben ab
        /// </summary>
        public static bool DeductFromUserCredit(decimal cost)
        {
            var currentCredit = Preferences.Get(Preferences_key__user_credit, "0");
            decimal credit = Convert.ToDecimal(currentCredit, CultureInfo.GetCultureInfo("en-US"));

            if (credit >= cost)
            {
                credit -= cost;
                Preferences.Set(Preferences_key__user_credit, credit.ToString(CultureInfo.GetCultureInfo("en-US")));
                return true;
            }
            else
            {
                Preferences.Set(Preferences_key__user_credit, "0");
                return false; // Nicht genug Guthaben
            }
        }

        public static decimal GetCurrentCredit() =>
            Preferences.Get(Preferences_key__user_credit, 0);

        public static void AddCredit(decimal amount, bool rewrite)
        {
            decimal credit = 0;
            if(!rewrite)
            {
                credit = Convert.ToDecimal(Preferences.Get(Preferences_key__user_credit, 0), CultureInfo.GetCultureInfo("en-US"));
            }
            Preferences.Set(Preferences_key__user_credit, (credit + amount).ToString(CultureInfo.GetCultureInfo("en-US")));
        }
    }
}
