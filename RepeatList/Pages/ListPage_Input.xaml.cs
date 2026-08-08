using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using RepeatList.ViewModels;


namespace RepeatList;

public partial class ListPage_Input : Popup<object>
{
    private enum DeepSeekType
    {
        Unknown,
        General,
        Spotify,
    }

    ListsPageViewModel listsPageViewModel = new();
    private readonly IRewardedAdService? _rewardedAd = IPlatformApplication.Current?.Services.GetService<IRewardedAdService>();

    private bool m_byChat;

    public bool IsOKClicked { get; private set; }

    public ListPage_Input()
    {
        InitializeComponent();
    }

    public ListPage_Input(bool isDeepSeekAllowed, bool byChat)
    {
        InitializeComponent();

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

        // A user-configured API key bypasses the internal billing gate.
        if (!AiSettingsService.Instance.HasSavedSettings)
        {
            await PromptForAiSettingsAsync();
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

    private async Task PromptForAiSettingsAsync()
    {
        bool go = await Shell.Current.DisplayAlert(
            AiSettingsService.T("AiSettingsTitle"),
            AiSettingsService.T("AiSettingsMissing"),
            AiSettingsService.T("AiSettingsOpen"),
            AiSettingsService.T("Cancel"));

        if (go)
            await Shell.Current.GoToAsync("//SetupPage");
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

    private void UpdateStatusLabel()
    {
        // Mit eigenem KI-Key sind alle KI-Funktionen aktiv; ohne Key nur der Aktivieren-Button.
        bool hasKey = AiSettingsService.Instance.HasSavedSettings;

        AiEnableHintLabel.Text = AiSettingsService.T("AiSettingsMissing");
        AiEnableButton.Text = AiSettingsService.T("AiEnableButton");
        AiEnableBox.IsVisible = !hasKey;

        DeepSeekEditor.IsVisible = hasKey;
        OnDeepSeekButton.IsVisible = hasKey;
        OnDeepSeek_SpotifyButton.IsVisible = false;
        if (!hasKey)
            DeepSeekEditor.Text = string.Empty;
    }

    private async void OnAiEnableClicked(object sender, EventArgs e)
    {
        // Popup schließen, dann zur Einstellungsseite navigieren
        await CloseMe("");
        await Shell.Current.GoToAsync("//SetupPage");
    }

    async Task CloseMe(dynamic param)
    {
        try
        {
            // Popup<TResult>.CloseAsync(param) sets the result and closes the popup.
            await CloseAsync(param);
        }
        catch (InvalidOperationException) when (Navigation.ModalStack.Any())
        {
            // CommunityToolkit.Maui throws PopupBlockedException (internal) when another
            // modal (e.g. the AI-unlock popup) is on top of the modal stack. Pop the
            // topmost modal and try closing again.
            await Navigation.PopModalAsync();
            await CloseAsync(param);
        }
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