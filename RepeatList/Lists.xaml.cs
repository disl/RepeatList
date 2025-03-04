using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RepeatList
{
    public partial class Lists : ContentPage
    {
        public SetupPageViewModel SetupPageViewModel { get; set; }
        public MainPageViewModel ViewModel { get; set; }

        public Lists()  //ISpeechToText speechToText)
        {
            InitializeComponent();

            ViewModel = new MainPageViewModel();
            BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            ViewModel.IsBusy=true;

            if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
                HeaderListView.SelectedItem=ViewModel.Headers[0];

            SetupPageViewModel = new SetupPageViewModel();
            if (SetupPageViewModel.SelectedItem != null)
            {
                SetCurrentCulture(SetupPageViewModel.SelectedItem.DefaultLanguage);
            }

            ViewModel.ItemSource_KindOfSorting = new ObservableCollection<CMBType_String>
            {
             new CMBType_String(Properties.Resources.sort_by_date, "date"),
             new CMBType_String(Properties.Resources.sort_by_alphabet, "alpha" )
            };
            //KindOfSortingPicker.ItemsSource = ViewModel.ItemSource_KindOfSorting.ToObservableCollection();

            var _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name, "date");
            if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
            else
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == "date");
            //KindOfSortingPicker.SelectedIndex = ViewModel.ItemSource_KindOfSorting.IndexOf(ViewModel.SelectedItem_KindOfSorting);

            ViewModel.IsBusy=false;

            string tmp_lists = Properties.Resources.Lists.ToUpper();

            ViewModel.Label_lists = tmp_lists;

            ViewModel.InitLabels();

            ViewModel.IsExpander_listsExpended=false;
            ViewModel.IsExpander_listsExpended=true;
        }

        private void SetCurrentCulture(string curr_culture)
        {
            //CultureInfo ci = new CultureInfo("en");
            var ci = new CultureInfo(curr_culture);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        #region HEADER

        private async void OnAddHeaderClicked(object sender, EventArgs e)
        {
            string new_list_name = await DisplayPromptAsync(Properties.Resources.Input, Properties.Resources.Enter_new_list_name, "OK", Properties.Resources.Cancel);
            if (!string.IsNullOrWhiteSpace(new_list_name))
            {
                var new_id = await ViewModel.AddHeader(new_list_name);
                ViewModel.SetFirstItemForHeaders();
            }
        }

        private async void HeaderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.IsBusy=true;

            ViewModel.HeaderSelected=true;

            var _selectedHeader = e.CurrentSelection[0] as Header;
            if (_selectedHeader != null)
            {
                ViewModel.Header_SelectedItem= _selectedHeader;

                await ViewModel.LoadPositions();
            }

            ViewModel.IsBusy=false;
        }

        private async void OnHeaderSelected(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void OnDeleteHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Delete_list, Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {

                    ViewModel.IsBusy=true;

                    await ViewModel.DeleteHeader(header);
                    ViewModel.SetFirstItemForHeaders();

                    await ViewModel.DeleteHeaderInSupabase(header);

                    await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_was_successfully_deleted);

                    ViewModel.IsBusy=false;
                }
            }
        }

        private async void OnCopyHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync(Properties.Resources.copy_list, Properties.Resources.Enter_list_name, "OK", Properties.Resources.Cancel);
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    var new_int = await ViewModel.CopyHeader(header, new_list_name);
                    ViewModel.SetFirstItemForHeaders();
                }
            }
        }

        private async void OnEditHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync(Properties.Resources.Input, Properties.Resources.Enter_new_list_name, "OK", Properties.Resources.Cancel, initialValue: header.ListName);
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    await ViewModel.EditNameHeader(header, new_list_name);
                }
            }
        }

        #endregion





        #region Coffee

        private async void CoffeeButtonClicked(object sender, EventArgs e)
        {
            string url = "https://Ko-fi.com/disl";
            await Launcher.OpenAsync(new Uri(url));
        }


        #endregion


        private async void OnIsSynchronizedClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Do_you_really_want_to_end_the_synchronising_of_this_list,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    await ViewModel.EditIsSynchronizedHeader(header, false);
                }
            }
        }

        private async void Sync_list_upClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;

                    await ViewModel.Sync_list_upClicked();
                }
            }
        }

        private async void Sync_list_downClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;

                    await ViewModel.Sync_list_downClicked(header);

                }
            }
        }




    }
}

