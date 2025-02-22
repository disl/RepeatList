using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RepeatList.ViewModels
{
    public partial class InputTextWithMicrophoneViewModel : ObservableObject // INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        private string label_Cancel = Properties.Resources.Cancel;
        public string Label_Cancel { get => label_Cancel; set => SetProperty(ref label_Cancel, value); }

        [ObservableProperty] string inputText;
        //public string InputText { get => inputText; set => SetProperty(ref inputText, value); }

        [ObservableProperty] string labelReplaceText = RepeatList.Properties.Resources.Replace_old_list_element_when_inserting;
        // public string LabelReplaceText { get => labelReplaceText; set => SetProperty(ref labelReplaceText, value); }


        [ObservableProperty] string labelText = Properties.Resources.InputTextWithMicrophoneViewModel_PlaceholderText;
        //public string LabelText { get => labelText; set => SetProperty(ref labelText, value); }

        [ObservableProperty] public string label_paste_from_clipboard = Properties.Resources.Paste_from_clipboard;

        bool replace_old_word_when_inserting;
        public bool Replace_old_word_when_inserting
        {
            get
            {
                replace_old_word_when_inserting =Preferences.Get("Replace_old_word_when_inserting", true);
                return replace_old_word_when_inserting;
            }
            set
            {
                replace_old_word_when_inserting = value;
                Preferences.Set("Replace_old_word_when_inserting", value);
                SetProperty(ref replace_old_word_when_inserting, value);
            }
        }

        #region COMMANDS

        [RelayCommand]
        public async Task Paste_from_clipboard() //string ExportedList, string Title)
        {

            var clipboard_text = await Clipboard.Default.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboard_text))
            {
                InputText=string.Empty;
                InputText = clipboard_text;
            }
            else
            {
                await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.The_clipboard_is_empty);
            }
        }

        #endregion

    }
}
