using RepeatList.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RepeatList.ViewModels
{
    public class InputTextWithMicrophoneViewModel : INotifyPropertyChanged
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

        private string label_Cancel = Resources.Cancel;
        public string Label_Cancel { get => label_Cancel; set => SetProperty(ref label_Cancel, value); }

        private string inputText;
        public string InputText { get => inputText; set => SetProperty(ref inputText, value); }

        private string labelReplaceText = Resources.Replace_old_list_element_when_inserting;
        public string LabelReplaceText { get => labelReplaceText; set => SetProperty(ref labelReplaceText, value); }


        private string labelText = Resources.InputTextWithMicrophoneViewModel_PlaceholderText;
        public string LabelText { get => labelText; set => SetProperty(ref labelText, value); }

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

    }
}
