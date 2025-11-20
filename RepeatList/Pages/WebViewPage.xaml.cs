using RepeatList.ViewModels;

namespace RepeatList.Pages;

public partial class WebViewPage : ContentPage
{
	public WebViewPage(string url)
	{
		InitializeComponent();

        BindingContext = new WebViewViewModel(url);
    }
}