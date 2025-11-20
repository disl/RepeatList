using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using RepeatList.Models;
using RepeatList.Pages;
using RepeatList.Services;
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
        private readonly InAppBillingService _billingService = new();

        private IDispatcherTimer _timer;


        //private readonly ISpeechToText _speechToText;

        public ListsPage()
        {
            try
            {
                InitializeComponent();

                ViewModel = new ListsPageViewModel();
                BindingContext = ViewModel;


            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        protected async override void OnAppearing()
        {
            try
            {
                ViewModel.IsBusy = true;

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


                // Prüfe ob Intent-Daten vorhanden sind
                var pendingJson = MainActivity.GetPendingIntentData();
                if (!string.IsNullOrEmpty(pendingJson))
                {
                    await ViewModel.LoadItemsFromJson(pendingJson);
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

            try
            {
                if (ViewModel.FilteredList == null || ViewModel.FilteredList.Count == 0)
                    return;

                // Collection kopieren um Enumeration-Fehler zu vermeiden
                var headersCopy = ViewModel.FilteredList.ToList();

                foreach (var header in headersCopy)
                {
                    try
                    {
                        if (header.IsSynchronized)
                        {
                            await ViewModel.Sync_list_downClicked(header.Id.ToString());
                            Header.IsSupabaseOk = true;  // Direkt im Header-Objekt setzen
                        }
                    }
                    catch (Exception headerEx)
                    {
                        // Fehler pro Header loggen, aber fortfahren
                        Header.IsSupabaseOk = false;
                        SentrySdk.CaptureException(headerEx);

                        // Optional: Kurze Pause zwischen Fehlern
                        await Task.Delay(1000);
                    }
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

        //private async Task ForTimer_Tick()
        //{
        //    ViewModel.IsBusy = true;

        //    if (ViewModel.FilteredList == null || ViewModel.FilteredList.Count == 0)
        //        return;

        //    try
        //    {
        //        foreach (var header in ViewModel.FilteredList.ToList())
        //        {
        //            if (header.IsSynchronized)
        //            {
        //                await ViewModel.Sync_list_downClicked(header.Id);
        //                Header.IsSupabaseOk = true;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SentrySdk.CaptureException(ex);
        //        Header.IsSupabaseOk = false;
        //        throw;
        //    }
        //    ViewModel.IsBusy = false;
        //}

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
            await ForOnAddHeaderClicked(false);
        }

        private async void OnAddHeaderByChatClicked(object sender, EventArgs e)
        {
            await ForOnAddHeaderClicked(true);
        }

        private async Task ForOnAddHeaderClicked(bool ByChat)
        {
            Guid tmp_guid = Guid.Empty;
            bool is_json = false;

            try
            {
                var isDeepSeekAllowed = await IsDeepSeekAllowed();

                var popup = new ListPage_Input(isDeepSeekAllowed, ByChat);
                var options = new PopupOptions
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
                };
                var new_list_name_obj = await PopupExtensions.ShowPopupAsync<object>(this, popup, options);

                if (new_list_name_obj.Result == null || string.IsNullOrEmpty(new_list_name_obj.Result.ToString()))
                    return;

                // Import JSON-File
                if (new_list_name_obj.Result.ToString() == "Start_import_file")
                {
                    ViewModel.IsBusy = true;

                    try
                    {
                        await DisplayAlert("Select JSON-File", Properties.Resources.Now_select_an_exported_JSON_file_named,
                            Properties.Resources.yes);

#if ANDROID
                        var activity = Platform.CurrentActivity;
                        activity?.FinishAffinity(); // Schließt alle Activities in der Task
#endif

                        //_ = ViewModel.Import_list_fileAsync();
                    }
                    finally
                    {
                        ViewModel.IsBusy = false; // Busy-State immer zurücksetzen
                    }
                    return;
                }

                var new_list_name = new_list_name_obj.Result;

                if (new_list_name is ChatResponseType.Root)
                {
                    // DeepSeek-List
                    var deeepseek_obj = new_list_name as ChatResponseType.Root;
                    if (deeepseek_obj != null && (deeepseek_obj.Header == null || deeepseek_obj.Items == null))
                    {
                        await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.The_chat_response_was_unsuccessful,
                                    visualOptions: new SnackbarOptions
                                    {
                                        BackgroundColor = Color.FromArgb(Constantes.Color_Warning_string),
                                        TextColor = Colors.White
                                    }, duration: TimeSpan.FromSeconds(7));
                        IsBusy = false;
                        return;
                    }

                    await ViewModel.InputHeaderWithPositionsDeepSeek(new_list_name as ChatResponseType.Root);
                    return;

                }
                else if (new_list_name is ChatResponse_SpotifyType.Root)
                {
                    // Spotify-List
                    var spotify_obj = new_list_name as ChatResponse_SpotifyType.Root;
                    if (spotify_obj != null && (spotify_obj.Header == null || spotify_obj.Items == null))
                    {
                        await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.The_chat_response_was_unsuccessful,
                                    visualOptions: new SnackbarOptions
                                    {
                                        BackgroundColor = Color.FromArgb(Constantes.Color_Warning_string),
                                        TextColor = Colors.White
                                    }, duration: TimeSpan.FromSeconds(7));
                        IsBusy = false;
                        return;
                    }
                    await ViewModel.InputHeaderWithPositionsSpotify(new_list_name as ChatResponse_SpotifyType.Root);
                    return;
                }
                else if (Guid.TryParse(new_list_name.ToString(), out tmp_guid))
                {
                    await ViewModel.Sync_list_downClicked(new_list_name.ToString());
                    return;
                }

                // Check ">>>" (JSON-List)
                else if (new_list_name.ToString().Contains(">>>"))
                {
                    var ind = new_list_name.ToString().IndexOf(">>>");
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
                    new_list_name = new_list_name.ToString().Substring(ind, new_list_name.ToString().Length - ind).Replace(">>>", "");

                    is_json = true;
                }

                if (!await ViewModel.InputHeaderWithPositions(new_list_name.ToString(), is_json))
                {
                    if (is_json)
                    {
                        await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_list_description,
                                    visualOptions: new SnackbarOptions
                                    {
                                        BackgroundColor = Color.FromArgb(Constantes.Color_Error_string),
                                        TextColor = Colors.White
                                    }, duration: TimeSpan.FromSeconds(3));
                        IsBusy = false;
                        return;
                    }
                    await ViewModel.AddHeader(new_list_name.ToString(), false);
                }
                await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_added_successfully,
                    visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                        TextColor = Colors.White
                    }, duration: TimeSpan.FromSeconds(2));

                ViewModel.SetFirstItemForHeaders();
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert(Properties.Resources.Error, ex.Message + Environment.NewLine +
                    "--- Stack Trace ---" + Environment.NewLine + ex.StackTrace,
                        Properties.Resources.yes);
#else
                await DisplayAlert(Properties.Resources.Error, ex.Message,
                        Properties.Resources.yes);
#endif

                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        async Task<bool> IsDeepSeekAllowed()
        {
            //var deviceID = GetDeviceID();
            var CanExecutePremium = await _billingService.CanExecuteQueryAsync();
            var IsFreeLimitReached = _billingService.IsFreeLimitReached();
            //////return (deviceID != null && ViewModel.DeviceList.Contains(deviceID)) || CanExecutePremium;
            //return  CanExecutePremium;
            return CanExecutePremium || !IsFreeLimitReached;
        }

        string? GetDeviceID()
        {
#if ANDROID

            var deviceId = Android.Provider.Settings.Secure.GetString(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId
            );

            return deviceId;
#endif

        }

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
                await DisplayAlert(Properties.Resources.Error, ex.Message,
                         Properties.Resources.yes);
            }
        }

        #endregion


        #region Coffee

        private async void CoffeeButtonClicked(object sender, EventArgs e)
        {
            string url = "https://ko-fi.com/disl";

            try
            {
                bool opened = await Launcher.TryOpenAsync(url);

                if (!opened)
                {
                    // Opening WebViewPage as fallback
                    await Navigation.PushAsync(new WebViewPage(url));
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                await DisplayAlert(Properties.Resources.Error, ex.Message, Properties.Resources.yes);
            }
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
            try
            {
                if (sender is Border border && border.BindingContext is Header selectedItem)
                {

                    ViewModel.Header_SelectedItem = selectedItem;

                    await Navigation.PushAsync(new PositionsPage(selectedItem));
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert(Properties.Resources.Error, ex.Message + Environment.NewLine +
                    "--- Stack Trace ---" + Environment.NewLine + ex.StackTrace,
                        Properties.Resources.yes);
#else
                await DisplayAlert(Properties.Resources.Error, ex.Message,
                        Properties.Resources.yes);
#endif
            }
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
            try
            {
                if (sender is ImageButton button)
                {
                    ViewModel.Header_SelectedItem = button.CommandParameter as Header;

                    var popup = new Lists_PopUpMenu((Header)button.CommandParameter, ViewModel.SupabaseService_ready);
                    var result = await Shell.Current.ShowPopupAsync<string>(popup);

                    //ViewModel.IsBusy = true;

                    switch (result.Result)
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
                            await ForOnAddHeaderClicked(false); break;
                        //case "Import_list_file":
                        //    await ForOnAddHeader_list_fileClicked(); break;
                        case "Export":
                            await ViewModel.Export_list_Clicked(); break;
                        case "ExportSpotify":
                            await ViewModel.Export_list_Spotify_Clicked(); break;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert(Properties.Resources.Error, ex.Message + Environment.NewLine +
                    "--- Stack Trace ---" + Environment.NewLine + ex.StackTrace,
                        Properties.Resources.yes);
#else
                await DisplayAlert(Properties.Resources.Error, ex.Message,
                        Properties.Resources.yes);
#endif
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }

        private async Task ForOnAddHeader_list_fileClicked()
        {
            await ViewModel.Import_list_fileAsync();
        }

        private async Task ForOnAddHeader_JSONClicked(bool v)
        {
            throw new NotImplementedException();
        }

        private void OnDeepseekClicked(object sender, EventArgs e)
        {

        }


    }
}

