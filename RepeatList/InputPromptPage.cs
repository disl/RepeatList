using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using System.Globalization;

namespace RepeatList;

public class InputPromptPage : ContentPage
{
    private readonly TaskCompletionSource<string> _completionSource;
    private Editor _editor;
    private Label _previewLabel;
    //private string RecognitionText;
    private CancellationToken cancellationToken;
    private readonly ISpeechToText _speechToText;

    public Task<string> Result => _completionSource.Task;

    public InputPromptPage(ISpeechToText speechToText)
    {
        this._speechToText=speechToText;

        _completionSource = new TaskCompletionSource<string>();

        _editor = new Editor { Placeholder = "Gib etwas ein...", AutoSize = EditorAutoSizeOption.TextChanges };
        _previewLabel = new Label { Text = "Vorschau: ", FontAttributes = FontAttributes.Bold };
        var microphoneButton = new Button { Text = "Spracheingabe" };
        var okButton = new Button { Text = "OK" };

        _editor.TextChanged += (s, e) => _previewLabel.Text = "Vorschau: " + _editor.Text;

        microphoneButton.Clicked += StartListening;

        okButton.Clicked += async (s, e) =>
        {
            _completionSource.SetResult(_editor.Text);
            await Navigation.PopModalAsync();
        };

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Children = { _editor, _previewLabel, microphoneButton, okButton }
        };
        
    }

    async void StartListening(object? sender, EventArgs e)
    {
        var isGranted = await _speechToText.RequestPermissions(cancellationToken);
        if (!isGranted)
        {
            await Toast.Make("Permission not granted").Show(CancellationToken.None);
            return;
        }

        _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;

        SpeechToTextOptions options = new SpeechToTextOptions() { Culture= CultureInfo.CurrentCulture.Parent };
        await _speechToText.StartListenAsync(options, CancellationToken.None);
    }

    async Task StopListening(CancellationToken cancellationToken)
    {
        await _speechToText.StopListenAsync(CancellationToken.None);
        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    }

    void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    {
        _editor.Text += args.RecognitionResult;
    }

    void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        _editor.Text = args.RecognitionResult.Text;
    }

    //public async Task StartSpeechToTextAsync()
    //{
    //    var cancellationToken = new CancellationTokenSource().Token;

    //    // Berechtigungen anfordern
    //    var isGranted = await SpeechToText.Default.RequestPermissions(cancellationToken);
    //    if (!isGranted)
    //    {
    //        Console.WriteLine("Berechtigungen nicht erteilt.");
    //        return;
    //    }

    //    SpeechToTextOptions options = new SpeechToTextOptions() { Culture= CultureInfo.CurrentCulture };
    //    var recognitionResult = await SpeechToText.StartListenAsync(
    //        options);

    //    // Ergebnis anzeigen
    //    if (recognitionResult.IsSuccessful)
    //    {
    //        Console.WriteLine($"Erkannter Text: {recognitionResult.Text}");
    //    }
    //    else
    //    {
    //        Console.WriteLine($"Fehler: {recognitionResult.Exception?.Message}");
    //    }
    //}

    //public async void Listen(object? sender, EventArgs e)
    //{
    //    var isGranted = await speechToText.RequestPermissions(cancellationToken);
    //    if (!isGranted)
    //    {
    //        await Toast.Make("Permission not granted").Show(CancellationToken.None);
    //        return;
    //    }

    //    await speechToText.StartListenAsync(new SpeechToTextOptions
    //    {
    //        Culture = CultureInfo.CurrentCulture,
    //        ShouldReportPartialResults = true
    //    }, CancellationToken.None);
    //}



    //async Task StartListening(CancellationToken cancellationToken)
    //{
    //    var isGranted = await speechToText.RequestPermissions(cancellationToken);
    //    if (!isGranted)
    //    {
    //        await Toast.Make("Permission not granted").Show(CancellationToken.None);
    //        return;
    //    }

    //    speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
    //    speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;
    //    await speechToText.StartListenAsync(new SpeechToTextOptions { 
    //        Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true }, CancellationToken.None);
    //}

    //async Task StopListening(CancellationToken cancellationToken)
    //{
    //    await speechToText.StopListenAsync(CancellationToken.None);
    //    speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
    //    speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    //}

    //void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    //{
    //    RecognitionText += args.RecognitionResult;
    //}

    //void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    //{
    //    RecognitionText = args.RecognitionResult.Text;
    //}
}
