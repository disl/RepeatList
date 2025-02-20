using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.ViewModels;


namespace RepeatList;

public partial class InputTextWithMicrophone : ContentPage
{
    private InputTextWithMicrophoneViewModel ViewModel { get; set; }
    private readonly TaskCompletionSource<string> _completionSource;
    public Task<string> Result => _completionSource.Task;
    private CancellationToken cancellationToken;

    public InputTextWithMicrophone()
    {
        InitializeComponent();

        ViewModel = new InputTextWithMicrophoneViewModel();
        BindingContext = ViewModel;

        //ViewModel = BindingContext as  InputTextWithMicrophoneViewModel;
        _completionSource = new TaskCompletionSource<string>();
    }

    private async void OkButton_Clicked(object sender, EventArgs e)
    {
        var json = JsonConvert.DeserializeObject<List<Position>>(ViewModel.InputText);
        
        if (json != null)
        {
            _completionSource.SetResult(ViewModel.InputText);
        }
        else if (ViewModel.InputText.Contains(",") && !ViewModel.InputText.Contains(",,"))
        {
            var is_ok = await DisplayAlert(
                Properties.Resources.Input, Properties.Resources.Do_you_want_to_use_commas_as_list_element_separators,
                Properties.Resources.yes, Properties.Resources.no);
            if (is_ok)
            {
                _completionSource.SetResult(ViewModel.InputText.Replace(",", ",,"));
            }
            else
                _completionSource.SetResult(ViewModel.InputText);
        }
        else
            _completionSource.SetResult(ViewModel.InputText);
        await Navigation.PopModalAsync();
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        _completionSource.SetResult(null);
        await Navigation.PopModalAsync();
    }

    private void OnDoubleCommaButton_Clicked(object sender, EventArgs e)
    {
        ViewModel.InputText += ",,";
    }

    #region microphon_backup

    //private readonly TaskCompletionSource<string> _completionSource;
    //private Editor _editor;
    //private Label _previewLabel;
    //private string RecognitionText;
    //private CancellationToken cancellationToken;
    //private readonly ISpeechToText _speechToText;

    //public InputTextWithMicrophone(ISpeechToText speechToText)
    //{
    //    InitializeComponent();

    //    ViewModel = BindingContext as  InputTextWithMicrophoneViewModel;

    //    //this._speechToText=speechToText;

    //    _completionSource = new TaskCompletionSource<string>();

    //    //_editor = new Editor { Placeholder = "Gib etwas ein...", AutoSize = EditorAutoSizeOption.TextChanges };
    //    //_previewLabel = new Label { Text = "Vorschau: ", FontAttributes = FontAttributes.Bold };
    //    //var microphoneButton = new Button { Text = "Spracheingabe" };
    //    //var okButton = new Button { Text = "OK" };
    //}

    //async void OnMicrophone_Clicked(object? sender, EventArgs e)
    //{
    //    var isGranted = await _speechToText.RequestPermissions(cancellationToken);
    //    if (!isGranted)
    //    {
    //        await Toast.Make("Permission not granted").Show(CancellationToken.None);
    //        return;
    //    }

    //    _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
    //    _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;

    //    SpeechToTextOptions options = new SpeechToTextOptions()
    //    {
    //        Culture= CultureInfo.CurrentCulture.Parent
    //    };
    //    await _speechToText.StartListenAsync(options, CancellationToken.None);
    //}

    //async Task StopListening(CancellationToken cancellationToken)
    //{
    //    await _speechToText.StopListenAsync(CancellationToken.None);
    //    _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
    //    _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    //}

    //void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    //{
    //    ViewModel.InputText += args.RecognitionResult;
    //}

    //void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    //{
    //    ViewModel.InputText = args.RecognitionResult.Text;
    //}

    #endregion
}