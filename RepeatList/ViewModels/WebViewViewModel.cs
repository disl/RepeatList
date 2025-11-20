using CommunityToolkit.Mvvm.ComponentModel;

namespace RepeatList.ViewModels
{
    public partial class WebViewViewModel : ObservableObject
    {
        

        [ObservableProperty]
        private string url = "about:blank";

        public WebViewViewModel(string targetUrl)
        {
            Url = targetUrl;
        }


    }
}

