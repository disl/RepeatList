using CommunityToolkit.Maui.Views;
using IntelliJ.Lang.Annotations;
using RepeatList.Models;
using RepeatList.Services;
using System.Text.Json;

namespace RepeatList;

public partial class ListPage_Input : Popup<object>
{
    public ListPage_Input()
    {
        InitializeComponent();
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

    private void OnDeepSeekClicked(object sender, EventArgs e)
    {
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

        var prompt = $"JSON list for a {DeepSeekEditor.Text} with the desired nested structure " +
            $"\"root (thema, description (short sequence or recipe)), items (item + quantity)\", in {language}. Do not translate structure!";

        Task.Run(async () =>
        {
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
                        var json = response.Substring(json_start_ind, json_end_ind - json_start_ind);
                        json = json.Replace("json","").Replace("```","").Trim();
                        var jsonObject = JsonSerializer.Deserialize<ChatResponseType.Root>(json);
                        if (jsonObject != null)
                        {
                            CloseAsync(jsonObject);
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
}