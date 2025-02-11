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


        private string labelText= Resources.InputTextWithMicrophoneViewModel_PlaceholderText;
        public string LabelText { get => labelText; set => SetProperty(ref labelText, value); }


    }
}
