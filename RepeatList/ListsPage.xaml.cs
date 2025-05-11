using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using Header = RepeatList.Models.Header;


namespace RepeatList
{
    public partial class ListsPage : ContentPage
    {
        int m_max_count_of_sync_lists = 2;
        static bool m_need_for_update = true;

        private SetupPageViewModel SetupPageViewModel { get; set; }
        private ListsPageViewModel ViewModel { get; set; }
        private ResourcesViewModel ResourcesViewModel { get; set; }

        private IDispatcherTimer _timer;

        public ListsPage()
        {
            InitializeComponent();

            ViewModel = new ListsPageViewModel();
            BindingContext = ViewModel;
        }

        protected async override void OnAppearing()
        {
            ViewModel.IsBusy = true;

            try
            {
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

                string tmp_lists = Properties.Resources.Lists.ToUpper();

                ViewModel.Label_lists = tmp_lists;

                ViewModel.InitLabels();

                ResourcesViewModel = new ResourcesViewModel();

                await ForTimer_Tick();

                if (m_need_for_update)
                {
#if ANDROID
                    if (IsPlayCoreApiAvailable())
                    {
                        await CheckForUpdates();
                    }       
#endif
                    m_need_for_update = false;
                }
            }
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }


#if ANDROID
        bool IsPlayCoreApiAvailable()
        {
            try
            {
                var context = Android.App.Application.Context;
                var packageManager = context.PackageManager;
                var playStorePackageName = "com.android.vending";
                var intent = packageManager.GetLaunchIntentForPackage(playStorePackageName);
                return intent != null;
            }
            catch
            {
                return false;
            }


        }


        private async Task CheckForUpdates()
        {
            try
            {
                var updater = new Platforms.Android.InAppUpdater();
                await updater.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {

                //SentrySdk.CaptureException(ex);
                //await Shell.Current.DisplayAlert("Update Error", ex.Message, "OK");

            }

        }
#endif




        private async void _timer_Tick(object? sender, EventArgs e)
        {
            await ForTimer_Tick();
        }

        private async Task ForTimer_Tick()
        {
            ViewModel.IsBusy = true;

            if (ViewModel.FilteredList == null || ViewModel.FilteredList.Count == 0)
                return;

            try
            {
                foreach (var header in ViewModel.FilteredList)
                {
                    if (header.IsSynchronized)
                    {
                        await ViewModel.Sync_list_downClicked(header.Id);
                        Header.IsSupabaseOk = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Header.IsSupabaseOk = false;
                throw;
            }

            ViewModel.IsBusy = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_timer != null)
                _timer.Stop(); // Timer anhalten, wenn die Seite nicht mehr sichtbar ist
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

            try
            {
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
                                   BackgroundColor = Color.FromArgb(Constantes.Color_Error_string),
                                   TextColor = Colors.White
                               }, duration: TimeSpan.FromSeconds(2));
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
                            BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                            TextColor = Colors.White
                        }, duration: TimeSpan.FromSeconds(2));

                    ViewModel.SetFirstItemForHeaders();
                }
            }
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
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
            try
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
                        await ViewModel.DeleteHeaderInSupabase(header);

                        await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_was_successfully_deleted,
                            visualOptions: new SnackbarOptions
                            {
                                BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                                TextColor = Colors.White
                            }, duration: TimeSpan.FromSeconds(2));

                        ViewModel.IsBusy = false;
                    }
                }
            }
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void OnCopyHeaderClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async Task OnEditHeaderClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
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
            try
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
            catch (Exception ex)
            {

                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async Task Sync_list_upClicked(object sender, EventArgs e)
        {
            try
            {
                var button = sender as ImageButton;
                if (button?.CommandParameter is Header header)
                {
                    var count_of_sync_lists = ViewModel.Headers.Count(x => x.IsSynchronized);

                    if (count_of_sync_lists == m_max_count_of_sync_lists)
                    {
                        await DisplayAlert(Properties.Resources.A_maximum_of_3_synchronised_lists_are_permitted.Replace("%1", m_max_count_of_sync_lists.ToString()),
                            Properties.Resources.Are_you_sure, Properties.Resources.yes);
                        return;
                    }

                    bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                        Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                    if (answer)
                    {
                        ViewModel.Header_SelectedItem = header;

                        await ViewModel.Sync_list_upClicked();
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async Task Sync_deleteClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Delete_Synchronisation,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;

                    await ViewModel.Sync_deleteClicked();
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
                var popup = new Lists_PopUpMenu((Header)button.CommandParameter, ViewModel.SupabaseService_ready);
                var result = await Shell.Current.ShowPopupAsync(popup);
                switch (result)
                {
                    case "Edit":
                        await OnEditHeaderClicked(button, e); break;
                    case "Delete":
                        await OnDeleteHeaderClicked(button, e); break;

                    case "SyncUp":
                        await Sync_list_upClicked(button, e); break;
                    case "SyncDelete":
                        await Sync_deleteClicked(button, e); break;

                    case "Import":
                        await ForOnAddHeaderClicked(); break;
                    case "Export":
                        await ViewModel.Export_list_Clicked(); break;
                }

            }
        }

    }
}

