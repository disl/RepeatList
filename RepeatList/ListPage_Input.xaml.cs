using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using RepeatList.ViewModels;
using static Android.Renderscripts.ScriptGroup;

namespace RepeatList;

public partial class ListPage_Input : Popup<object>
{
    private const string DailyQueryCountKey = "QueriesToday";

    ListsPageViewModel listsPageViewModel = new();
    private readonly InAppBillingService _billing = new();

    bool m_isDeepSeekAllowed = false;
    private bool m_byChat;

    public bool IsOKClicked { get; private set; }

    public ListPage_Input()
    {
        InitializeComponent();
    }

    public ListPage_Input(bool isDeepSeekAllowed, bool byChat)
    {
        InitializeComponent();

        m_isDeepSeekAllowed = isDeepSeekAllowed;
        m_byChat = byChat;

        UpdateStatusLabel();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseMe("");
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        string input = ListNameEditor.Text?.Trim();
        await CloseMe(input);
    }

    private async void OnDeepSeekClicked(object sender, EventArgs e)
    {
        var deviceID = GetDeviceID();
        var CanExecutePremium = await _billing.CanExecuteQueryAsync();
        var CanExecuteQueryForFree = await _billing.CanExecuteQueryForFreeAsync(true);


        if (!CanExecuteQueryForFree && !CanExecutePremium)
        {
            await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Free_daily_limit_reached, "OK");
            return;
        }

        if (deviceID != null && !listsPageViewModel.DeviceList.Contains(deviceID) && !CanExecutePremium && !CanExecuteQueryForFree)
        {
            await Shell.Current.DisplayAlert(Properties.Resources.premium_feature, Properties.Resources.Only_available_in_the_premium_version, "OK");
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

        var prompt = $"JSON list for a '{DeepSeekEditor.Text}'. Complete recipe as text with the desired nested structure. " +
            $"\"Header (Title + Description + Sequence_text) and Items (Description + Quantity)\", in {language}. Do not translate structure! " +
            $"Only if it is a cooking recipe, state the number of people the recipe is intended for in the description. " +
            $"JSON must not contain any strings that interfere with JSON parsing.";

        string json;

        try
        {
            var response = await client.GetCompletionAsync(prompt);

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                await Shell.Current.DisplayAlert("Error", "No response from DeepSeek.", "OK");
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (!response.Content.Contains("```json"))
                            return;

                        var json_start_ind = response.Content.IndexOf("```json");
                        var json_end_ind = response.Content.LastIndexOf("```");
                        if (json_start_ind < 0 || json_end_ind < 0 || json_end_ind <= json_start_ind)
                        {
                            await Shell.Current.DisplayAlert("Error", "Invalid response format from DeepSeek.", "OK");
                            return;
                        }
                        json = response.Content.Substring(json_start_ind, json_end_ind - json_start_ind);
                        json = json.Replace("json", "").Replace("```", "").Trim();
                        var jsonObject = JsonConvert.DeserializeObject<ChatResponseType.Root>(json);

                        if (jsonObject != null)
                        {
                            // Sortierung nach Alphabet
                            Preferences.Set(listsPageViewModel.SelectedItem_KindOfSorting_key_name_undone, "alpha");

                            UpdateStatusLabel();

                            await CloseMe(jsonObject); 
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert("Error", "Invalid JSON structure from DeepSeek.", "OK");

                            m_isDeepSeekAllowed = false;
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
        catch (CreditIsInsufficientError)
        {
            m_isDeepSeekAllowed = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.insufficient_credit, "OK");
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shell.Current.DisplayAlert(Properties.Resources.Error, ex.Message, "OK");
            });
        }
        finally
        {
            Activity_Indicator.IsEnabled = false;
            Activity_Indicator.IsRunning = false;

            //UpdateStatusLabel();
        }
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
        if (IsOKClicked) return;

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
        try
        {
            if (await _billing.PurchaseTokenPackAsync())
            {
                await Shell.Current.DisplayAlert(Properties.Resources.Thank_You, Properties.Resources.Thank_you_for_your_purchase, "OK");
                m_isDeepSeekAllowed = true;
            }
            else
            {
                await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Purchase_failed_Try_again, "OK");
                m_isDeepSeekAllowed = false;
            }
            UpdateStatusLabel();
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            });
        }
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
        var available_tokens = _billing.GetAvailableTokens();
        var available_tokens_percent = Convert.ToDouble(available_tokens / InAppBillingService.TokenPackAmount_199);

        string status = _billing.HasActiveSubscription()
            ? Properties.Resources.Premium_active
            : $"{Properties.Resources.The_remaining_credit_is.Replace("%1", (available_tokens_percent * 100.0).ToString("N0")) + "%"}";

        StatusLabel.IsVisible = true;
        StatusLabel.Text = status;
        FreeStatusLabel.IsVisible = false;
        FreeStatusLabel.Text = "";
        OnDeepSeekButton.IsVisible = m_isDeepSeekAllowed;  // Ok-Button 

        var DailyQueryCount = Preferences.Get(DailyQueryCountKey, 0);

        // TEST !!!
        //DailyQueryCount = 3; // For testing purposes, set to 3

        if (DailyQueryCount >= 0 && DailyQueryCount < 3)
        {
            FreeStatusLabel.Text = $"{Properties.Resources.Free_queries}: {(3 - DailyQueryCount).ToString()}";
            FreeStatusLabel.IsVisible = true;
            StatusLabel.IsVisible = false;
        }

        OnDeepSeekButton.IsVisible = m_isDeepSeekAllowed || !StatusLabel.IsVisible;  //m_isDeepSeekAllowed && (DailyQueryCount < 3);  // Ok-Button        
        DeepSeekEditor.IsVisible = OnDeepSeekButton.IsVisible;
        if (!DeepSeekEditor.IsVisible)
            DeepSeekEditor.Text = string.Empty;

        BuyTokenPackButton.IsVisible = !OnDeepSeekButton.IsVisible;



        //BuySubscriptionButton.IsVisible = !isDeepSeekAllowed;
        //RestorePurchasesButton.IsVisible = !isDeepSeekAllowed;                
    }

    async Task CloseMe(dynamic param)
    {
        // Popup zuerst schließen
        if (Handler != null)
        {
            CloseAsync(param); // Bei Popup<TResult> -> kein await, sofort Ergebnis setzen
        }

        // Danach Navigation
        if (Navigation.ModalStack.Any())
        {
            await Navigation.PopModalAsync();
        }

        //if (!Navigation.ModalStack.Any())
        //{
        //    await CloseAsync(param);
        //}
        //else
        //{
        //    await Navigation.PopModalAsync();
        //    await CloseAsync(param);
        //}
    }
}