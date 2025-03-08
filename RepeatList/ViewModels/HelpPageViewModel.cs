using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace RepeatList.ViewModels
{
    public partial class HelpPageViewModel : ObservableObject
    {
        [ObservableProperty] private HelpTopic helpTopic ;
        [ObservableProperty] private string title;
        [ObservableProperty] public HelpTopicThemasEnum thema;
        [ObservableProperty] private CultureInfo cultur;


        public enum HelpTopicThemasEnum
        {
            InputTextBox,
        }

        public HelpPageViewModel() {  }

        public HelpPageViewModel(HelpTopicThemasEnum thema, CultureInfo curr_culture)
        {


            Thema = thema;

            Cultur = CultureInfo.CurrentCulture;
        }

        public void SetHelpTopic()
        {
            Title = Properties.Resources.help.ToUpper();

            switch (Thema)
            {
                case HelpTopicThemasEnum.InputTextBox:
                    HelpTopic = new HelpTopic
                    {
                        Title = Properties.Resources.Create_new_article,
                        Content = Properties.Resources.InputTextWithMicrophoneViewModel_PlaceholderText
                    };
                    break;
            }
        }

        [RelayCommand]
        public async Task GoBack()
        {
            if (Application.Current.MainPage is NavigationPage navPage)
            {
                await navPage.Navigation.PopAsync();
            }
            else
            {
                switch(Thema)
                {
                    case HelpTopicThemasEnum.InputTextBox:
                        await Shell.Current.GoToAsync("//Lists/Positions");
                        break;
                    default:
                        await Shell.Current.GoToAsync("//Lists");
                        break;
                }
            }
        }
    }

    public class HelpTopic
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
