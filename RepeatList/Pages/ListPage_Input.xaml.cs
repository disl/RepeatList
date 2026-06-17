using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using RepeatList.ViewModels;


namespace RepeatList;

public partial class ListPage_Input : Popup<object>
{
    private const string DailyQueryCountKey = "QueriesToday";
    private enum DeepSeekType
    {
        Unknown,
        General,
        Spotify,
    }

    ListsPageViewModel listsPageViewModel = new();
    private readonly InAppBillingService _billing = new();
    private readonly IRewardedAdService? _rewardedAd = IPlatformApplication.Current?.Services.GetService<IRewardedAdService>();

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
        bool flowControl = await ForOnDeepSeekClicked(DeepSeekType.General);
        if (!flowControl)
        {
            return;
        }
    }

    private async Task<bool> ForOnDeepSeekClicked(DeepSeekType Mode = DeepSeekType.Unknown)
    {
        string prompt = string.Empty;
        var deviceID = GetDeviceID();
        var CanExecutePremium = await _billing.CanExecuteQueryAsync();
        var CanExecuteQueryForFree = await _billing.CanExecuteQueryForFreeAsync(true);

        if (!CanExecuteQueryForFree && !CanExecutePremium)
        {
            if (_rewardedAd != null && _rewardedAd.IsAdReady)
            {
                bool watchAd = await Shell.Current.DisplayAlert(
                    Properties.Resources.Free_daily_limit_reached,
                    Properties.Resources.Watch_ad_for_extra_queries,
                    Properties.Resources.Watch_Ad,
                    Properties.Resources.Cancel);

                if (watchAd)
                {
                    bool rewarded = await _rewardedAd.ShowRewardedAdAsync();
                    if (rewarded)
                    {
                        _billing.AddAdBonus(5);
                        // retry with bonus
                        CanExecuteQueryForFree = await _billing.CanExecuteQueryForFreeAsync(true);
                    }
                }
            }
            else
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Free_daily_limit_reached, "OK");
            }

            if (!CanExecuteQueryForFree && !CanExecutePremium)
                return false;
        }

        if (deviceID != null && !listsPageViewModel.DeviceList.Contains(deviceID) && !CanExecutePremium && !CanExecuteQueryForFree)
        {
            if (Shell.Current  != null)
                await Shell.Current.DisplayAlert(Properties.Resources.premium_feature, Properties.Resources.Only_available_in_the_premium_version, "OK");
            return false;
        }

        if (string.IsNullOrEmpty(DeepSeekEditor.Text))
            return false;

        if (Thread.CurrentThread.CurrentCulture == null)
            return false;

        Activity_Indicator.IsEnabled = true;
        Activity_Indicator.IsRunning = true;

        var client = new DeepSeekClient();
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

        if (Mode == DeepSeekType.Spotify)
            prompt = $"Create a JSON list for a Spotify playlist based on the following description: '{DeepSeekEditor.Text}'. " +
                $"The JSON should have a \"Header\" (title, description), and an \"Items\" array with each track's (artist, title). " +
                $"Format the response in English. Do not translate the JSON structure! " +
                $"The fields 'artist' and 'title' must not contain any '-'. These would then have to be replaced with ' '. " +
                $"Ensure the JSON is properly formatted and does not contain any extraneous text or characters that could interfere with parsing. " +
                $"Do not use sublists.";
        else
            prompt = $"JSON list for a '{DeepSeekEditor.Text}'. Complete recipe as text with the desired nested structure. " +
            $"\"Header (Title + Description + Sequence_text) and Items (Description + Quantity)\", in {language}. Do not translate structure! " +
            $"Only if it is a cooking recipe, state the number of people the recipe is intended for in the description. " +
            $"JSON must not contain any strings that interfere with JSON parsing.Do not use sublists.";

        string json = string.Empty;

        try
        {
            var response = await client.GetCompletionAsync(prompt, "application/json");

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                if (Shell.Current  != null)
                    await Shell.Current.DisplayAlert("Error", "No response from DeepSeek.", "OK");
                return false;
            }

            json=ForForOnDeepSeekClicked(Mode, json, response);
        }
        catch (CreditIsInsufficientError)
        {
            m_isDeepSeekAllowed = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Shell.Current  != null)
                    Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.insufficient_credit, "OK");
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Shell.Current  != null)
                    Shell.Current.DisplayAlert(Properties.Resources.Error, ex.Message, "OK");
            });
        }
        finally
        {
            Activity_Indicator.IsEnabled = false;
            Activity_Indicator.IsRunning = false;

            //UpdateStatusLabel();
        }

        return true;
    }

    private string ForForOnDeepSeekClicked(DeepSeekType Mode, string json, CompletionResult response)
    {


        MainThread.BeginInvokeOnMainThread(async () =>
        {
            dynamic? jsonObject = null;

            try
            {
                if (!response.Content.Contains("```json") && !IsJSONString(response.Content))
                    return;

                if (Mode == DeepSeekType.General || Mode == DeepSeekType.Spotify)
                {
                    var json_start_ind = response.Content.IndexOf("```json");
                    var json_end_ind = response.Content.LastIndexOf("```");
                    if (json_start_ind < 0 || json_end_ind < 0 || json_end_ind <= json_start_ind)
                    {
                        if (Shell.Current  != null)
                            await Shell.Current.DisplayAlert("Error", "Invalid response format from DeepSeek.", "OK");
                        return;
                    }
                    json = response.Content.Substring(json_start_ind, json_end_ind - json_start_ind);
                    json = json.Replace("json", "").Replace("```", "").Trim();

                    switch (Mode)
                    {
                        case DeepSeekType.Spotify:
                            jsonObject = JsonConvert.DeserializeObject<ChatResponse_SpotifyType.Root>(json);
                            break;
                        case DeepSeekType.General:
                            jsonObject = JsonConvert.DeserializeObject<ChatResponseType.Root>(json);
                            break;
                    }
                }
                else if (IsJSONString(response.Content))
                {
                    json = response.Content;
                }
                else
                {
                    if (Shell.Current  != null)
                        await Shell.Current.DisplayAlert("Error", "Invalid response format from DeepSeek (2).", "OK");
                    return;
                }

                if (jsonObject != null)
                {
                    // Sortierung nach Alphabet
                    Preferences.Set(listsPageViewModel.SelectedItem_KindOfSorting_key_name_undone, "alpha");

                    UpdateStatusLabel();

                    await CloseMe(jsonObject);
                }
                else
                {
                    if (Shell.Current  != null)
                        await Shell.Current.DisplayAlert("Error", "Invalid JSON structure from DeepSeek.", "OK");

                    m_isDeepSeekAllowed = false;
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Shell.Current  != null)
                        Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                });
            }
        });
        return json;
    }

    private bool IsJSONString(string content)
    {
        bool ret_val = false;
        try
        {
            var obj = JsonConvert.DeserializeObject(content);
            ret_val = true;
        }
        catch
        {
            ret_val = false;
        }
        return ret_val;
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
        //OnDeepSeekClicked(sender, e);
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
        if (Shell.Current  != null)
        {
            if (await _billing.PurchaseSubscriptionAsync())
                await Shell.Current.DisplayAlert(Properties.Resources.Success, Properties.Resources.Premium_subscription_activated, "OK");
            else
                await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Purchase_failed_Try_again, "OK");
        }
        UpdateStatusLabel();
    }

    private async void OnBuyTokenPack(object sender, EventArgs e)
    {
        try
        {
            if (await _billing.PurchaseTokenPackAsync())
            {
                if (Shell.Current  != null)
                    await Shell.Current.DisplayAlert(Properties.Resources.Thank_You, Properties.Resources.Thank_you_for_your_purchase, "OK");
                m_isDeepSeekAllowed = true;
            }
            else
            {
                if (Shell.Current  != null)
                    await Shell.Current.DisplayAlert(Properties.Resources.Error, Properties.Resources.Purchase_failed_Try_again, "OK");
                m_isDeepSeekAllowed = false;
            }
            UpdateStatusLabel();
        }
        catch (Exception ex)
        {
            var shell = Shell.Current;
            if (shell != null)
                await shell.DisplayAlert("Error", ex.Message, "OK");
            else
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnRestorePurchases(object sender, EventArgs e)
    {
        bool restored = await _billing.RestorePurchasesAsync();
        if (Shell.Current  != null)
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

        if (DailyQueryCount >= 0 && DailyQueryCount < InAppBillingService.FreeDailyLimit)
        {
            FreeStatusLabel.Text = $"{Properties.Resources.Free_queries}: {(InAppBillingService.FreeDailyLimit - DailyQueryCount)}";
            FreeStatusLabel.IsVisible = true;
            StatusLabel.IsVisible = false;
        }

        OnDeepSeekButton.IsVisible = m_isDeepSeekAllowed || !StatusLabel.IsVisible;
        OnDeepSeek_SpotifyButton.IsVisible = false;
        DeepSeekEditor.IsVisible = OnDeepSeekButton.IsVisible;
        if (!DeepSeekEditor.IsVisible)
            DeepSeekEditor.Text = string.Empty;
        BuyTokenPackButton.IsVisible = !OnDeepSeekButton.IsVisible;
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

    private async void OnDeepSeek_SpotifyClicked(object sender, EventArgs e)
    {
        bool flowControl = await ForOnDeepSeekClicked(DeepSeekType.Spotify);
        if (!flowControl)
        {
            return;
        }
    }

    private async void OnListImport_Clicked(object sender, EventArgs e)
    {
        //await listsPageViewModel.Import_list_fileAsync();

        await CloseMe("Start_import_file");
    }
}