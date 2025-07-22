using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class ListPage_Input : Popup<object>
{
    ListsPageViewModel listsPageViewModel = new();    
    private readonly InAppBillingService _billing = new();
    bool isDeepSeekAllowed = false;

    public ListPage_Input()
    {
        InitializeComponent();
    }

    public ListPage_Input(bool isDeepSeekAllowed)
    {
        InitializeComponent();

        this.isDeepSeekAllowed=isDeepSeekAllowed;
        OnDeepSeekButton.IsVisible = isDeepSeekAllowed;
        //BuySubscriptionButton.IsVisible = !isDeepSeekAllowed;
        BuyTokenPackButton.IsVisible = !isDeepSeekAllowed;
        //RestorePurchasesButton.IsVisible = !isDeepSeekAllowed;

        DeepSeekEditor.IsEnabled = isDeepSeekAllowed;

        UpdateStatusLabel();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        CloseAsync("");
    }

    private void OnOkClicked(object sender, EventArgs e)
    {
        string input = ListNameEditor.Text?.Trim();
        CloseAsync(input);
    }

    private async void OnDeepSeekClicked(object sender, EventArgs e)
    {
        var deviceID = GetDeviceID();
        var CanExecutePremium = await _billing.CanExecuteQueryAsync(true);

        if (deviceID != null && !listsPageViewModel.DeviceList.Contains(deviceID) && !CanExecutePremium)
        {
            Shell.Current.DisplayAlert(Properties.Resources.premium_feature, Properties.Resources.Only_available_in_the_premium_version, "OK");
            return;
        }

        if (string.IsNullOrEmpty(DeepSeekEditor.Text))
            return;

        if (Thread.CurrentThread.CurrentCulture == null)
            return;

        Activity_Indicator.IsEnabled = true;
        Activity_Indicator.IsRunning = true;

        var client = new DeepSeekClient("sk-a3240964efda4aa1aa6cf6ffcf9713b2");
        var language = "English";

        switch (Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName)
        {
            case "en": language = "English"; break;
            case "es": language = "Spanish"; break;
            case "fr": language = "French"; break;
            case "de": language = "German"; break;
            case "it": language = "Italian"; break;
            case "uk": language = "Ukrainian"; break;
            case "ru": language = "Russian"; break;
        }

        var prompt = $"JSON list for a {DeepSeekEditor.Text}. Complete recipe as text with the desired nested structure. " +
            $"\"Header (Title + Description + Sequence_text) and Items (Description + Quantity)\", in {language}. Do not translate structure! " +
            $"If this is a recipe, enter the number of people in the description.";

        Task.Run(async () =>
        {
            string json;

            try
            {
                string response = await client.GetCompletion(prompt);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (!response.Contains("```json"))
                            return;

                        var json_start_ind = response.IndexOf("```json");
                        var json_end_ind = response.LastIndexOf("```");
                        if (json_start_ind < 0 || json_end_ind < 0 || json_end_ind <= json_start_ind)
                        {
                            Shell.Current.DisplayAlert("Error", "Invalid response format from DeepSeek.", "OK");
                            return;
                        }
                        json = response.Substring(json_start_ind, json_end_ind - json_start_ind);
                        json = json.Replace("json", "").Replace("```", "").Trim();
                        var jsonObject = JsonConvert.DeserializeObject<ChatResponseType.Root>(json);

                        if (jsonObject != null)
                        {
                            CloseAsync(jsonObject);

                            // Sortierung nach Alphabet
                            Preferences.Set(listsPageViewModel.SelectedItem_KindOfSorting_key_name_undone, "alpha");
                        }
                        else
                        {
                            Shell.Current.DisplayAlert("Error", "Invalid JSON structure from DeepSeek.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                });
            }
            finally
            {
                Activity_Indicator.IsEnabled = false;
                Activity_Indicator.IsRunning = false;
            }
        });
    }

    string? GetDeviceID()
    {
#if ANDROID

        var deviceId = Android.Provider.Settings.Secure.GetString(
            Android.App.Application.Context.ContentResolver,
            Android.Provider.Settings.Secure.AndroidId
        );

        return deviceId;
#endif

    }

    private void DeepSeekEditor_Completed(object sender, EventArgs e)
    {
        OnDeepSeekClicked(sender, e);
    }

    private void ListNameEditor_Completed(object sender, EventArgs e)
    {
        OnOkClicked(sender, e);
    }

    //private async void OnPayPremiumClicked(object sender, EventArgs e)
    //{
    //    await _billingService.PayPremiumMonthAsync();
    //}

    private async void OnBuySubscription(object sender, EventArgs e)
    {
        if (await _billing.PurchaseSubscriptionAsync())
            await Shell.Current.DisplayAlert(Properties.Resources.Success, Properties.Resources.Premium_subscription_activated, "OK");
        else
            await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Purchase_failed_Try_again, "OK");

        UpdateStatusLabel();
    }

    private async void OnBuyTokenPack(object sender, EventArgs e)
    {
        if (await _billing.PurchaseTokenPackAsync())
            await Shell.Current.DisplayAlert(Properties.Resources.Thank_You, Properties.Resources.You_received__1_query_tokens.Replace("%1", "1000"), "OK");
        else
            await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Purchase_failed_Try_again, "OK");

        UpdateStatusLabel();
    }

    private async void OnRestorePurchases(object sender, EventArgs e)
    {
        bool restored = await _billing.RestorePurchasesAsync();
        await Shell.Current.DisplayAlert(Properties.Resources.Restore, 
            restored ? Properties.Resources.Premium_restored : Properties.Resources.No_purchases_found, "OK");
        UpdateStatusLabel();
    }

    private void UpdateStatusLabel()
    {
        string status = _billing.HasActiveSubscription()
            ? "🌟 " + Properties.Resources.Premium_active
            : $" 🔄 {Properties.Resources.Premium_active}: {_billing.GetAvailableTokens()}";

        StatusLabel.Text = status;
    }
}