using RepeatList.ViewModels;
using System.Globalization;
using static RepeatList.ViewModels.HelpPageViewModel;

namespace RepeatList;

public partial class HelpPage : ContentPage
{
    private HelpPageViewModel ViewModel { get; set; }

    public HelpPage()
	{
		InitializeComponent();
	}

    public HelpPage(HelpTopicThemasEnum thema, CultureInfo culture)
    {
        InitializeComponent();

        ViewModel = new HelpPageViewModel(thema, culture);
        BindingContext = ViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.SetHelpTopic();
    }
}