using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using Header = RepeatList.Models.Header;
// Remove the incorrect using directive
// using Google.MobileAds;

// Add the correct using directive for Google AdMob
//using Google.Ads.Mediation;
//using Android.Gms.Ads;



namespace RepeatList
{
    public partial class ListsPage : ContentPage
    {
        private SetupPageViewModel SetupPageViewModel { get; set; }
        private ListsPageViewModel ViewModel { get; set; }
        private ResourcesViewModel ResourcesViewModel { get; set; }

        public ListsPage()
        {
            InitializeComponent();

            ViewModel = new ListsPageViewModel();
            BindingContext = ViewModel;

            //var adRequest = new AdRequest.Builder().Build();
            //adView.LoadAd(adRequest);
        }

        protected async override void OnAppearing()
        {
            ViewModel.IsBusy = true;

            //if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
            //    HeaderListView.SelectedItem=ViewModel.Headers[0];

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

            var _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name, "date");
            if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
            else
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == "date");

            ViewModel.IsBusy = false;

            string tmp_lists = Properties.Resources.Lists.ToUpper();

            ViewModel.Label_lists = tmp_lists;

            ViewModel.InitLabels();

            ResourcesViewModel = new ResourcesViewModel();

            //ViewModel.IsExpander_listsExpended=false;
            //ViewModel.IsExpander_listsExpended=true;
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
            await ForOnAddHeaderClicked();
        }

        private async Task ForOnAddHeaderClicked()
        {
            Guid tmp_guid = Guid.Empty;

            string new_list_name = await DisplayPromptAsync(
                Properties.Resources.AddNewList, Properties.Resources.Enter_a_list_name_or_insert_a_ready_made, "OK", Properties.Resources.Cancel);
            if (!string.IsNullOrWhiteSpace(new_list_name))
            {
                if (Guid.TryParse(new_list_name, out tmp_guid))
                {
                    await ViewModel.Sync_list_downClicked(new_list_name);
                    return;
                }

                // Check ">>>" (JSON-List)
                else if (new_list_name.Contains(">>>"))
                {
                    var ind = new_list_name.IndexOf(">>>");
                    if (ind < 0)
                    {
                        await Application.Current.MainPage.DisplaySnackbar(
                           Properties.Resources.List_information_has_wrong_format, visualOptions: new SnackbarOptions
                           {
                               BackgroundColor = Color.FromArgb(Constantes.Color_Error),
                               TextColor = Colors.White
                           });
                        return;
                    }
                    new_list_name = new_list_name.Substring(ind, new_list_name.Length - ind).Replace(">>>", "");
                }

                if (!await ViewModel.InputHeaderWithPositions(new_list_name))
                {
                    var new_id = await ViewModel.AddHeader(new_list_name, false);
                }
                await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_added_successfully,
                    visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb(Constantes.Color_Success),
                        TextColor = Colors.White
                    });

                ViewModel.SetFirstItemForHeaders();
            }
        }

        //private async void HeaderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ViewModel.IsBusy=true;
        //    ViewModel.HeaderSelected=true;

        //    var _selectedHeader = e.CurrentSelection[0] as Header;
        //    if (_selectedHeader != null)
        //    {
        //        ViewModel.Header_SelectedItem= _selectedHeader;

        //        //await ViewModel.LoadPositions();

        //        await Navigation.PushAsync(new Positions(_selectedHeader));

        //        //await Shell.Current.GoToAsync("Positions");
        //    }
        //    ViewModel.IsBusy=false;
        //}


        private async Task OnDeleteHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Delete_list, Properties.Resources.Selected_list_and_list_synchronisations_will_now_be_deleted,
                    Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {

                    ViewModel.IsBusy = true;

                    await ViewModel.DeleteHeader(header);
                    ViewModel.SetFirstItemForHeaders();

                    // Delete linkd to Supabase 
                    //await ViewModel.DeleteHeaderInSupabase(header);


                    await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_was_successfully_deleted,
                        visualOptions: new SnackbarOptions
                        {
                            BackgroundColor = Color.FromArgb(Constantes.Color_Success),
                            TextColor = Colors.White
                        });

                    ViewModel.IsBusy = false;
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

        private async Task OnEditHeaderClicked(object sender, EventArgs e)
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

        private async Task Sync_list_upClicked(object sender, EventArgs e)
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

        private async Task Sync_list_downClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;
                    await ViewModel.Sync_list_downClicked(header.Id);
                }
            }
        }

        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            //ViewModel.IsBusy=true;

            //ViewModel.HeaderSelected=true;

            //var _selectedHeader = e. CurrentSelection[0] as Header;
            //if (_selectedHeader != null)
            if (sender is Border border && border.BindingContext is Header selectedItem)
            {

                ViewModel.Header_SelectedItem = selectedItem;

                //await ViewModel.LoadPositions();

                await Navigation.PushAsync(new PositionsPage(selectedItem));
            }

            //ViewModel.IsBusy=false;
        }

        private void OnHeaderListViewSelected(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? "";

            ViewModel.FilteredList.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                ViewModel.FilteredList = new ObservableCollection<Header>(ViewModel.Headers);
            }
            else
            {
                foreach (var item in ViewModel.Headers.Where(x => x.ListName.ToLower().Contains(searchText.ToLower())))
                {
                    ViewModel.FilteredList.Add(item);
                }
            }
        }

        private async void OnBurgerMenuTapped(object sender, TappedEventArgs e)
        {
            if (sender is ImageButton button)
            {
                var popup = new Lists_PopUpMenu((Header)button.CommandParameter);
                var result = await Shell.Current.ShowPopupAsync(popup);
                switch (result)
                {
                    case "Edit":
                        await OnEditHeaderClicked(button, e); break;
                    case "Delete":
                        await OnDeleteHeaderClicked(button, e); break;

                    case "SyncUp":
                        await Sync_list_upClicked(button, e); break;
                    case "SyncDown":
                        await Sync_list_downClicked(button, e); break;

                    case "Import":
                        await ForOnAddHeaderClicked(); break;
                    case "Export":
                        await ViewModel.Export_list_Clicked(); break;
                }

            }
        }

    }
}

