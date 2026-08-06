using CommunityToolkit.Maui.Views;
using RepeatList.Services;

namespace RepeatList;

/// <summary>
/// Result of the AI unlock popup: whether the user wants to open the settings
/// now and whether the "don't show again" checkbox was checked.
/// </summary>
public sealed record AiUnlockResult(bool GoToSettings, bool DontShowAgain);

/// <summary>
/// Popup that offers unlocking the AI features. "Now" opens the settings page,
/// "Later" closes it. With the "Don't show again" checkbox checked the popup
/// stays hidden on future app starts.
/// </summary>
public partial class AiUnlockPopup : Popup<AiUnlockResult>
{
    public AiUnlockPopup()
    {
        InitializeComponent();

        TitleLabel.Text = AiSettingsService.T("AiFeatureCardTitle");
        FeatureLabel.Text = "• " + AiSettingsService.T("AiFeatureListGen") +
                            "\n\n• " + AiSettingsService.T("AiFeatureVoiceInput");
        HintLabel.Text = AiSettingsService.T("AiEnableLaterHint");
        DontShowAgainLabel.Text = AiSettingsService.T("AiDontShowAgain");
        LaterButton.Text = AiSettingsService.T("AiLater");
        NowButton.Text = AiSettingsService.T("AiEnableNow");
    }

    // Tapping the label next to the checkbox toggles it as well.
    private void OnDontShowAgainTapped(object sender, EventArgs e)
    {
        DontShowAgainCheckbox.IsChecked = DontShowAgainCheckbox.IsChecked != true;
    }

    private async void OnNowClicked(object sender, EventArgs e)
    {
        await ClosePopupAsync(new AiUnlockResult(true, DontShowAgainCheckbox.IsChecked == true));
    }

    private async void OnLaterClicked(object sender, EventArgs e)
    {
        await ClosePopupAsync(new AiUnlockResult(false, DontShowAgainCheckbox.IsChecked == true));
    }

    private async Task ClosePopupAsync(AiUnlockResult result)
    {
        try
        {
            await CloseAsync(result, CancellationToken.None);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
