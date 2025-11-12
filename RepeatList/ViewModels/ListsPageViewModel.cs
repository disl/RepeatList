using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Position = RepeatList.Models.Position;

namespace RepeatList.ViewModels
{
    public partial class ListsPageViewModel : ObservableObject
    {
        private DatabaseService _databaseService;
        private SupabaseService? _supabaseService;
        private readonly SpotifyService _spotifyService = new SpotifyService();

        [ObservableProperty] public static List<string> deviceList = new();

        // File export
        private readonly FileExportService _fileExportService;

        [ObservableProperty] private bool _isExporting;

        [ObservableProperty] private string _exportStatus;

        private SetupPageViewModel? setupPageViewModel;

        public string SelectedItem_KindOfSorting_key_name = "SelectedItem_KindOfSorting";
        public string SelectedItem_KindOfSorting_key_name_undone = "SelectedItem_KindOfSorting_undone";
        public double ButtonsSize = 25;

        // [ObservableProperty] public bool deepSeekAllowed;
        //partial void OnDeepSeekAllowedChanged(bool oldValue, bool newValue)
        // {
        //     if (newValue)
        //     {
        //         deepSeekNotAllowed = false;
        //     }
        //     else
        //     {
        //         deepSeekNotAllowed = true;
        //     }
        // }

        // [ObservableProperty] public bool deepSeekNotAllowed;

        [ObservableProperty] public string resetImageSource;
        [ObservableProperty] public bool supabaseService_ready;

        public ListsPageViewModel()
        {
            _databaseService = new DatabaseService();
            _fileExportService = new();

            try
            {
                _supabaseService = new SupabaseService();
                SupabaseService_ready = true;
            }
            catch (Exception ex)
            {
                _supabaseService = null;
                SupabaseService_ready = false;
            }

            setupPageViewModel = new SetupPageViewModel();
            _ = setupPageViewModel.Load();
            CurrentCulture = setupPageViewModel.SelectedItem.DefaultLanguage;
            CultureInfo culture = new CultureInfo(CurrentCulture);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            _ = LoadHeaders();
            _ = GetDeviceIDs();

            SetFirstItemForHeaders();
            InitSelectedItem_KindOfSorting();
            InitSelectedItem_KindOfSorting_undone();
            SetResetImageSource();

            spotifyInfo = AppSettings.Load().Result.SpotifyInfo;
        }

        #region File export


        [RelayCommand]
        public async Task ExportListAsync(IEnumerable<string> items)
        {
            if (items == null || !items.Any())
            {
                await Shell.Current.DisplayAlert("Error", "No data to export", "OK");
                return;
            }

            IsExporting = true;
            ExportStatus = "Exportiere...";

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"meine_liste_{timestamp}.txt";

                var success = await _fileExportService.ExportToDownloadsAsync(filename, items);

                if (success)
                {
                    ExportStatus = "Export erfolgreich!";
                    await Shell.Current.DisplayAlert("Erfolg",
                        $"Datei wurde im Downloads-Ordner gespeichert:\n{filename}", "OK");
                }
                else
                {
                    ExportStatus = "Export fehlgeschlagen";
                    await Shell.Current.DisplayAlert("Error", "Export could not be performed", "OK");
                }
            }
            catch (Exception ex)
            {
                ExportStatus = "Fehler beim Export";
                await Shell.Current.DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
            }
            finally
            {
                IsExporting = false;
            }
        }

        // Für komplexe Objekte
        //[RelayCommand]
        public async Task ExportAsCsvAsync<T>(IEnumerable<T> items, string filename = "export.csv")
        {
            try
            {
                var csvContent = new StringBuilder();

                // Header
                var properties = typeof(T).GetProperties();
                var header = string.Join(",", properties.Select(p => p.Name));
                csvContent.AppendLine(header);

                // Daten
                foreach (var item in items)
                {
                    var values = properties.Select(p =>
                        $"\"{p.GetValue(item)?.ToString()?.Replace("\"", "\"\"")}\"");
                    csvContent.AppendLine(string.Join(",", values));
                }

                await _fileExportService.ExportToDownloadsAsync(filename, csvContent.ToString());
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }



        #endregion


        private async Task GetDeviceIDs()
        {
            IsBusy = true;

            List<DeviceList>? sync_responce = await _supabaseService.GetDeviceListAsync();

            if (sync_responce != null)
            {
                foreach (var item in sync_responce)
                {
                    if (DeviceList == null)
                        DeviceList = new List<string>();
                    if (!DeviceList.Contains(item.DeviceId))
                        DeviceList.Add(item.DeviceId);
                }
            }
            IsBusy = false;
        }

        public void InitLabels()
        {
            Cultur = CultureInfo.CurrentCulture;

            Title = Properties.Resources.Lists.ToUpper();
            Search = Properties.Resources.search;
            Label_Lists = Properties.Resources.Lists.ToUpper() + " (0)";
            Please_create_a_first_list = Properties.Resources.Please_create_a_first_list;
            Label_lists = Properties.Resources.Lists.ToUpper();
            Label_addNewList = Properties.Resources.AddNewList;
            Label_AddNewItem = Properties.Resources.AddNewItem;
            //Label_ResetLists = Properties.Resources.ResetLists;
            Label_Export_list = Properties.Resources.Export_list;
            Label_copy_list_to_clipboard = Properties.Resources.Copy_list_to_clipboard;
            Label_Reset_current_list = Properties.Resources.Reset_current_list;
            Label_done = Properties.Resources.done;
            Label_undone = Properties.Resources.undone;
            Label_paste_from_clipboard = Properties.Resources.Paste_from_clipboard;
            InputText_placeholder = Properties.Resources.InputText_placeholder;
        }

        private void SetResetImageSource()
        {
            string image_source = "disbale monetization in youtube channel.png";
            ResetImageSource = image_source;
        }

        private void InitSelectedItem_KindOfSorting()
        {
            string _selectedItem_KindOfSorting = Preferences.Get(SelectedItem_KindOfSorting_key_name, "date");
            if (_selectedItem_KindOfSorting == null)
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            else
                SelectedItem_KindOfSorting = ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting);  // new CMBType_String(Properties.Resources.sort_by, "date");
        }

        private void InitSelectedItem_KindOfSorting_undone()
        {
            string _selectedItem_KindOfSorting = Preferences.Get(SelectedItem_KindOfSorting_key_name, "date");
            if (_selectedItem_KindOfSorting == null)
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            else
                SelectedItem_KindOfSorting = ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting);  // new CMBType_String(Properties.Resources.sort_by, "date");
        }

        public void SetFirstItemForHeaders()
        {
            //if (Headers != null && Headers.Count > 0)
            if (Header_SelectedItem == null && Headers != null && Headers.Count > 0)
                Header_SelectedItem = Headers[0];
        }

        #region PROPERTIES

        //bool replace_old_word_when_inserting;
        //public bool Replace_old_word_when_inserting
        //{
        //    get
        //    {
        //        replace_old_word_when_inserting = Preferences.Get("Replace_old_word_when_inserting", true);
        //        return replace_old_word_when_inserting;
        //    }
        //    set { replace_old_word_when_inserting = value; }
        //}
        [ObservableProperty] public string sync_arrow_down_icon = "sync_arrow_down_icon_red.png";
        [ObservableProperty] public CultureInfo cultur;
        [ObservableProperty] ObservableCollection<Header>? filteredList = new();
        [ObservableProperty] public string please_create_a_first_list = Properties.Resources.Please_create_a_first_list;
        [ObservableProperty] public bool isSynchronized = false;
        [ObservableProperty] public string title_sort_by = Properties.Resources.sort_by;
        [ObservableProperty] public string title_KindOfSorting = "Sort";
        [ObservableProperty] public CMBType_String selectedItem_KindOfSorting;
        [ObservableProperty] public CMBType_String selectedItem_KindOfSorting_undone;
        [ObservableProperty]
        public ObservableCollection<CMBType_String> itemSource_KindOfSorting = new ObservableCollection<CMBType_String>
        {
             new CMBType_String(Properties.Resources.sort_by_date, "date"),
             new CMBType_String(Properties.Resources.sort_by_alphabet, "alpha" )
        };
        [ObservableProperty] public bool positionListViewVisible;
        [ObservableProperty] public bool headerSelected;
        [ObservableProperty] public string positionImageSource = "check_box_blank.png";
        [ObservableProperty] public bool changeListsCheckedState;
        partial void OnChangeListsCheckedStateChanged(bool oldValue, bool newValue)
        {
            string image_source = "";
            if (Application.Current.UserAppTheme == AppTheme.Dark)
            {
                image_source = newValue ? "check_box_check_white.png" : "check_box_blank_white.png";
            }
            else
            {
                image_source = newValue ? "check_box_check.png" : "check_box_blank.png";
            }

            PositionImageSource = image_source;
        }

        [ObservableProperty] private string search = Properties.Resources.search;
        [ObservableProperty] private string title = Properties.Resources.Lists.ToUpper();
        [ObservableProperty] private string currentCulture;
        [ObservableProperty] private int imageButton_size = 35;
        [ObservableProperty] private string label_lists = Properties.Resources.Lists.ToUpper();
        [ObservableProperty] private string label_addNewList = Properties.Resources.AddNewList;
        [ObservableProperty] private string label_Lists = Properties.Resources.Lists.ToUpper() + " (0)";
        [ObservableProperty] private string label_AddNewItem = Properties.Resources.AddNewItem;
        //[ObservableProperty] private string label_ResetLists = Properties.Resources.ResetLists;
        [ObservableProperty] private string _label_Export_list = Properties.Resources.Export_list;
        [ObservableProperty] private string label_copy_list_to_clipboard = Properties.Resources.Copy_list_to_clipboard;
        [ObservableProperty] private string _label_Reset_current_list = Properties.Resources.Reset_current_list;
        [ObservableProperty] private string _label_done = Properties.Resources.done;
        [ObservableProperty] private string _label_undone = Properties.Resources.undone;
        [ObservableProperty] private string label_paste_from_clipboard = Properties.Resources.Paste_from_clipboard;

        [ObservableProperty] private Header? header_SelectedItem;
        [ObservableProperty] private ObservableCollection<Header>? headers = new ObservableCollection<Header>();
        [ObservableProperty] private Header? header = new Header();
        [ObservableProperty] private Models.Position? position_selectedItem;

        [ObservableProperty] private ObservableCollection<Position> lists = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> lists_undone = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> lists_done = new ObservableCollection<Position>();

        public event PropertyChangedEventHandler _PropertyChanged;
        protected void OnPropertyChanged_(string propertyName)
        {
            _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ObservableProperty] public bool isBusy;
        [ObservableProperty] private string expander_listsIcon = "collapse_icon.png";
        [ObservableProperty] private string expander_ListsIcon = "collapse_icon.png";

        [ObservableProperty] public bool isExpander_listsExpended = false;
        [ObservableProperty] public bool isExpander_ListsExpended = true;

        //[ObservableProperty] public double ListsListHeight;

        public double IconsHeightRequested = 30;

        [ObservableProperty] private string inputText;
        [ObservableProperty] private string inputText_placeholder;

        private SpotifyInfo spotifyInfo;
        private HttpClient m_client;

        partial void OnIsExpander_listsExpendedChanged(bool oldValue, bool newValue)
        {
            Expander_listsIcon = newValue ? "collapse_icon.png" : "expand_icon.png";
            //ListsListHeight=newValue ? 400 : 600;
        }

        partial void OnIsExpander_ListsExpendedChanged(bool oldValue, bool newValue)
        {
            Expander_ListsIcon = newValue ? "collapse_icon.png" : "expand_icon.png";
        }

        #endregion


        #region COMMANDS       

        public async Task<bool> InputHeaderWithPositionsDeepSeek(ChatResponseType.Root input_object)
        {
            if (input_object == null || input_object.Header == null)
            {
                IsBusy = false;
                return false;
            }

            IsBusy = true;

            // Add new header
            var new_header = await AddHeader(input_object.Header.Title, false);
            Header_SelectedItem = new_header;

            // Add description
            var new_pos = new Position
            {
                Id = Guid.NewGuid().ToString(),
                HeaderId = new_header.Id,
                Title = "_" + Properties.Resources.description.ToUpper() + ": " + input_object.Header.Description + Environment.NewLine + input_object.Header.SequenceText,
                IsCompleted = false,
                UpdatedAt = DateTime.Now.ToUniversalTime()
            };
            await AddPosition(new_pos, false, false);

            // Add new positions
            foreach (var pos in input_object.Items)
            {
                new_pos = new Position
                {
                    Id = Guid.NewGuid().ToString(),
                    HeaderId = new_header.Id,
                    Title = pos.Description + " " + pos.Quantity,
                    IsCompleted = false,
                    UpdatedAt = DateTime.Now.ToUniversalTime()
                };
                await AddPosition(new_pos, false, false);
            }
            IsBusy = false;
            return false;
        }

        public async Task<bool> InputHeaderWithPositionsSpotify(ChatResponse_SpotifyType.Root input_object)
        {
            if (input_object == null || input_object.Header == null)
            {
                IsBusy = false;
                return false;
            }

            IsBusy = true;

            // Add new header
            var new_header = await AddHeader(input_object.Header.Title, false);
            Header_SelectedItem = new_header;

            // Add description
            var new_pos = new Position
            {
                Id = Guid.NewGuid().ToString(),
                HeaderId = new_header.Id,
                Title = "_" + Properties.Resources.description.ToUpper() + ": " + input_object.Header.Description,
                IsCompleted = false,
                UpdatedAt = DateTime.Now.ToUniversalTime()
            };
            await AddPosition(new_pos, false, false);

            // Add new positions
            foreach (var pos in input_object.Items)
            {
                new_pos = new Position
                {
                    Id = Guid.NewGuid().ToString(),
                    HeaderId = new_header.Id,
                    Title = pos.Artist + " - " + pos.Title,
                    IsCompleted = false,
                    UpdatedAt = DateTime.Now.ToUniversalTime()
                };
                await AddPosition(new_pos, false, false);
            }
            IsBusy = false;
            return false;
        }

        //[RelayCommand]
        public async Task<bool> InputHeaderWithPositions(string _input, bool is_json)
        {
            Header? json = null;

            IsBusy = true;

            if (string.IsNullOrEmpty(_input) || Headers == null)
            {
                IsBusy = false;
                return false;
            }

            try
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new OnlyPositionsJsonConverter());
                json = JsonConvert.DeserializeObject<Header>(_input, settings);
            }
            catch (Exception ex)
            {
                //if (ex != null)
                //    SentrySdk.CaptureException(ex);

                if (is_json)
                {
                    //await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_list_description);
                    //IsBusy = false;
                    return false;
                }
            }

            if (json != null)
            {
                if (Headers != null)
                {
                    var header = Headers.FirstOrDefault(x => x.Id == json.Id);
                    if (header != null)
                    {
                        Header_SelectedItem = header;

                        // Existing Header
                        header.UpdatedAt = DateTime.Now.ToUniversalTime();
                        // Add to existing positions
                        foreach (var pos in json.Positions)
                        {
                            pos.Title = pos.Title.Trim() + " (+)";
                            await AddPosition(pos, true, false);
                        }
                    }
                    else
                    {
                        // Add new header
                        json.UpdatedAt = DateTime.Now.ToUniversalTime();
                        var new_header = await AddHeader(json.ListName, false, json.Id);

                        Header_SelectedItem = new_header;

                        // Add new positions
                        foreach (var pos in json.Positions)
                        {
                            pos.HeaderId = new_header.Id;
                            pos.Title = pos.Title.Trim() + " (+)";
                            await AddPosition(pos, false, false);
                        }
                    }
                    IsBusy = false;
                    return true;
                }
            }
            IsBusy = false;
            return false;


        }






        //[RelayCommand]
        //public async Task Import_listClicked()
        //{
        //    Guid tmp_guid = Guid.Empty;

        //    string guid_str = await Application.Current.MainPage.DisplayPromptAsync(
        //         Properties.Resources.import_liste,
        //         Properties.Resources.Please_enter_the_ID_of_the_list_to_be_synchronised);
        //    if (!string.IsNullOrEmpty(guid_str))
        //    {
        //        if (!Guid.TryParse(guid_str, out tmp_guid))
        //        {
        //            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_List_ID);
        //            IsBusy = false;
        //            return;
        //        }
        //    }
        //    else
        //        return;

        //    IsBusy = true;

        //    (Header Header, List<Position> Lists) sync_responce = await _supabaseService.GetHeaderWithPositionsByIdAsync(tmp_guid);

        //    if (sync_responce.Header == null || sync_responce.Lists == null)
        //    {
        //        await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_not_available_or_corrupt);
        //        IsBusy = false;
        //        return;
        //    }

        //    var _header = Headers.FirstOrDefault(x => x.Id == sync_responce.Header.Id);
        //    if (_header != null)
        //        Header = sync_responce.Header;
        //    else
        //        Header = await AddHeader(sync_responce.Header.ListName, sync_responce.Header.Id);

        //    foreach (var pos in sync_responce.Lists)
        //    {
        //        await AddPosition(pos, false);
        //    }

        //    Header_SelectedItem = Header;
        //    await EditIsSynchronizedHeader(Header_SelectedItem, true);

        //    await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_was_imported_successfully);

        //    IsBusy = false;
        //}

        [RelayCommand]
        public async Task Sync_list_downClicked(string guid_str_param)  //Header header)
        {
            if (string.IsNullOrWhiteSpace(guid_str_param) || _supabaseService == null)
                return;

            Guid tmp_guid = new Guid(guid_str_param);

            IsBusy = true;

            (Header Header, List<Position> Position) sync_responce = await _supabaseService.GetHeaderWithPositionsByIdAsync(tmp_guid);

            if (sync_responce.Header == null || sync_responce.Position == null)
            {
                //string guid_str = await Application.Current.MainPage.DisplayPromptAsync(
                //  Properties.Resources.Would_you_like_to_work_with_someone_on_a_current_list,
                //  Properties.Resources.Please_enter_the_ID_of_the_list_to_be_synchronised);
                //if (!string.IsNullOrEmpty(guid_str))
                //{
                //    if (!Guid.TryParse(guid_str, out tmp_guid))
                //    {
                await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_List_ID,
                    visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb(Constantes.Color_Error_string),
                        TextColor = Colors.White
                    },
                duration: TimeSpan.FromSeconds(2));
                IsBusy = false;
                return;
                //}
                //}
            }

            Header? _header = null;
            if (Headers != null)
                _header = Headers.FirstOrDefault(x => x.Id == sync_responce.Header.Id);
            if (_header != null)
            {
                await EditNameHeader(_header, sync_responce.Header.ListName);

                Lists = (await _databaseService.GetPositionsAsync(_header.Id)).ToObservableCollection();

                foreach (var pos in sync_responce.Position)
                {
                    var old_pos = Lists.FirstOrDefault(p => p.Id == pos.Id);
                    if (old_pos == null)
                        await AddPosition(pos, false, true);
                    else
                        await UpdatePosition(pos);
                }
            }
            else
            {
                Header = await AddHeader(sync_responce.Header.ListName, true, sync_responce.Header.Id);
                foreach (var pos in sync_responce.Position)
                {
                    await AddPosition(pos, false, true);
                }
            }
            IsBusy = false;
        }

        [RelayCommand]
        public async Task Sync_list_upClicked()
        {
            if (_supabaseService == null)
            {
                //await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.Supabase_service_is_not_available);
                IsBusy = false;
                return;
            }
            Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
            if (Lists == null || Lists.Count == 0 || Header == null)
            {
                IsBusy = false;
                return;
            }

            IsBusy = true;

            Header_SelectedItem.Positions = Lists.ToList();

            if (SupabaseService_ready)
                await _supabaseService.SyncHeaderWithDetailsAsync(Header_SelectedItem);

            await EditIsSynchronizedHeader(Header_SelectedItem, true);

            await Sync_list_downClicked(Header_SelectedItem.Id);

            bool answer = await Application.Current.MainPage.DisplayAlert(
                Properties.Resources.Would_you_like_to_work_with_someone_on_a_current_list + Environment.NewLine +
                Properties.Resources.To_be_able_to_edit_the_list_please_use_the_following_key
                    .Replace("%1", Header_SelectedItem.Id).Replace("%2", Header_SelectedItem.ListName) + ":"
                , Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
            if (answer)
            {
                var share_text = Header_SelectedItem.Id;
                //Properties.Resources.To_be_able_to_edit_the_list_please_use_the_following_key
                //.Replace("%1", Header_SelectedItem.Id).Replace("%2", Header_SelectedItem.ListName);
                await Utilities.ShareTextAsync(share_text);
            }



            IsBusy = false;
        }

        [RelayCommand]
        public async Task Export_list_text_Clicked()
        {
            Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
            if (Lists == null || Lists.Count == 0 || Header == null)
            {
                IsBusy = false;
                return;
            }
            IsBusy = true;

            //Header header = Header_SelectedItem;
            //header.Positions = Lists.ToList();

            //var settings = new JsonSerializerSettings();
            //settings.Converters.Add(new OnlyPositionsJsonConverter());
            //var json = JsonConvert.SerializeObject(header, settings);

            //var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", json);



            string send_text = "";
            for (int i = 1; i <= Lists.Count; i++)
            {
                send_text += i + ". " + Lists[i].Title + Environment.NewLine;
            }


            await Utilities.ShareTextAsync(send_text);

            IsBusy = false;
        }

        [RelayCommand]
        //public async Task Export_list_Clicked()
        //{
        //    Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
        //    if (Lists == null || Lists.Count == 0 || Header == null)
        //    {
        //        IsBusy = false;
        //        return;
        //    }
        //    IsBusy = true;

        //    Header header = Header_SelectedItem;
        //    header.Positions = Lists.ToList();

        //    var settings = new JsonSerializerSettings();
        //    settings.Converters.Add(new OnlyPositionsJsonConverter());
        //    var json = JsonConvert.SerializeObject(header, settings);

        //    var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", json);

        //    await Utilities.ShareTextAsync(send_text);

        //    IsBusy = false;
        //}

        public async Task Export_list_Clicked()
        {
            Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
            if (Lists == null || Lists.Count == 0 || Header == null)
            {
                IsBusy = false;
                return;
            }
            IsBusy = true;

            try
            {
                Header header = Header_SelectedItem;
                header.Positions = Lists.ToList();

                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new OnlyPositionsJsonConverter());
                var json = JsonConvert.SerializeObject(header, settings);

                // 1. JSON-Datei erstellen
                string fileName = $"MiniList_Export.json";
                string filePath = await CreateJsonFile(json, fileName);

                if (!string.IsNullOrEmpty(filePath))
                {
                    // 2. Datei teilen über WhatsApp oder andere Apps
                    await ShareFileAsync(filePath, "Data export", "application/json");
                }
                else
                {
                    // Fallback: Text teilen (wie bisher)
                    var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", json);
                    await Utilities.ShareTextAsync(send_text);
                }
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung
                await Application.Current.MainPage.DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task Import_list_fileAsync()
        {
            try
            {
                IsBusy = true;

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // 30 Sekunden Timeout
                FileResult result = null;

                var jsonFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } }
                });

                try
                {
                    result =  await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Select JSON-File (MiniList_Export.json)",
                        FileTypes = jsonFileType
                    }).WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("FilePicker timeout oder abgebrochen");
                    await App.Current.MainPage.DisplayAlert("Timeout", "FilePicker wurde abgebrochen", "OK");
                    return;
                }

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    string json = await reader.ReadToEndAsync();

                    await LoadItemsFromJson(json);
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadItemsFromJson(string json)
        {
            IsBusy = true;

            try
            {
                if (await InputHeaderWithPositions(json, true))
                {
              

                    //IsBusy = true;

                    // UI sofort aktualisieren
                    //await MainThread.InvokeOnMainThreadAsync(() =>
                    //{
                    //    // Collection neu erstellen um UI-Update zu erzwingen
                    //    Headers = new ObservableCollection<Header>(headers);
                    //    FilteredList = new ObservableCollection<Header>(Headers);
                    //});
                    //MainThread.BeginInvokeOnMainThread(async () =>
                    //{




                    //await Application.Current.MainPage.DisplayAlert("Success", Properties.Resources.List_added_successfully, "OK");
                    //MainThread.BeginInvokeOnMainThread(async () =>
                    //{
                    //await LoadHeaders();

                    //await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_added_successfully,
                    //  visualOptions: new SnackbarOptions
                    //  {
                    //      BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                    //      TextColor = Colors.White
                    //  }, duration: TimeSpan.FromSeconds(2));


                    //});


                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Import failed: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<string> CreateJsonFile(string jsonContent, string fileName)
        {
            try
            {
                // Für Android
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Temporäres Verzeichnis verwenden
                    var tempDir = FileSystem.CacheDirectory;
                    var filePath = Path.Combine(tempDir, fileName);

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    await File.WriteAllTextAsync(filePath, jsonContent);
                    return filePath;
                }
                // Für iOS
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    var tempDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var filePath = Path.Combine(tempDir, fileName);

                    await File.WriteAllTextAsync(filePath, jsonContent);
                    return filePath;
                }
                // Für Windows
                else if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    var tempDir = Path.GetTempPath();
                    var filePath = Path.Combine(tempDir, fileName);

                    await File.WriteAllTextAsync(filePath, jsonContent);
                    return filePath;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Erstellen der JSON-Datei: {ex.Message}");
                return null;
            }
        }

        private async Task ShareFileAsync(string filePath, string title, string contentType)
        {
            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = title,
                    File = new ShareFile(filePath, contentType)
                });
            }
            catch (Exception ex)
            {
                // Fallback auf Text-Sharing
                Console.WriteLine($"Datei-Sharing fehlgeschlagen: {ex.Message}");
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", jsonContent);
                await Utilities.ShareTextAsync(send_text);
            }
        }



        [RelayCommand]
        public async Task Export_list_Spotify_Clicked()
        {
            Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
            if (Lists == null || Lists.Count == 0 || Header_SelectedItem == null)
            {
                IsBusy = false;
                await Application.Current.MainPage.DisplayAlert("Error", "No tracks or headers available.", "OK");
                return;
            }
            IsBusy = true;

            Header header = Header_SelectedItem;
            header.Positions = Lists.ToList();

            try
            {
                // OAuth-Flow
                var authUrl = "https://accounts.spotify.com/authorize" +
                    $"?client_id={spotifyInfo.ClientId}" +
                    "&response_type=code" +
                    "&redirect_uri=myapp://callback" +
                    "&scope=user-read-private%20user-read-email%20playlist-modify-private%20playlist-modify-public";

                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authUrl),
                    new Uri("myapp://callback")
                );

                var code = result.Properties["code"];
                var (accessToken, refreshToken) = await GetAccessTokenFromCode(
                    code,
                    spotifyInfo.ClientId,
                    spotifyInfo.ClientSecret,
                    "myapp://callback"
                );
                await SecureStorage.SetAsync("refresh_token", refreshToken); // Speichere Refresh Token

                // Erstelle Playlist
                var playlistId = await CreatePlaylistOnSpotify(accessToken);
                if (string.IsNullOrEmpty(playlistId))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Playlist could not be created.", "OK");
                    IsBusy = false;
                    return;
                }

                // Tracks aus header.Positions sammeln
                var trackTitles = header.Positions
                    .Where(p => !p.Title.StartsWith("_")) // Filtere Beschreibungen
                    .Select(p => p.Title)
                    .ToList();

                if (!trackTitles.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No valid tracks found.", "OK");
                    IsBusy = false;
                    return;
                }

                var trackUris = new List<string>();
                foreach (var track in trackTitles)
                {
                    // Verbessere Suchanfrage (z. B. Titel und Künstler splitten)
                    var parts = track.Split('-').Select(s => s.Trim()).ToArray();
                    var query = parts.Length > 1 ? $"artist:{parts[0]} track:{parts[1]}" : track;
                    var searchResponse = await SpotifyApiClient.ExecuteWithTokenRefresh(
                        async client => await client.GetAsync($"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1"),
                        accessToken,
                        RefreshAccessToken
                    );

                    if (searchResponse.IsSuccessStatusCode)
                    {
                        var searchJson = await searchResponse.Content.ReadAsStringAsync();
                        var searchResult = System.Text.Json.JsonDocument.Parse(searchJson);
                        var items = searchResult.RootElement.GetProperty("tracks").GetProperty("items");
                        if (items.GetArrayLength() > 0)
                        {
                            var trackId = items[0].GetProperty("id").GetString();
                            trackUris.Add($"spotify:track:{trackId}");
                        }
                        else
                        {
                            Console.WriteLine($"Kein Treffer für Track: {track}");
                        }
                    }
                    else
                    {
                        var errorContent = await searchResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Fehler bei Track-Suche für '{track}': {searchResponse.StatusCode}, Details: {errorContent}");
                    }
                    await Task.Delay(1000); // Rate-Limit respektieren
                }

                // Tracks zur Playlist hinzufügen
                if (trackUris.Any())
                {
                    var batches = trackUris.Chunk(100); // Max. 100 Tracks pro Request
                    foreach (var batch in batches)
                    {
                        var content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(new { uris = batch }),
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );
                        var response = await SpotifyApiClient.ExecuteWithTokenRefresh(
                            async client => await client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", content),
                            accessToken,
                            RefreshAccessToken
                        );
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            await Application.Current.MainPage.DisplayAlert("Error", $"Tracks could not be added: {errorContent}", "OK");
                            IsBusy = false;
                            return;
                        }
                    }
                    //Console.WriteLine($"{trackUris.Count} Successfully added tracks to playlist '{playlistId}' hinzugefügt.");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Warning", "No tracks found, empty playlist created.", "OK");
                }

                // Playlist öffnen
                string spotifyUri = $"spotify:playlist:{playlistId}";
                string httpsUri = $"https://open.spotify.com/playlist/{playlistId}";
                try
                {
                    await Launcher.OpenAsync(httpsUri);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Error opening playlist: {ex.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<(string AccessToken, string RefreshToken)> GetAccessTokenFromCode(
            string code, string clientId, string clientSecret, string redirectUri)
        {
            using var client = new HttpClient();
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };
            var content = new FormUrlEncodedContent(requestBody);
            var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Fehler beim Token-Austausch: {response.StatusCode}, Details: {errorContent}");
            }
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonDocument.Parse(json);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            var refreshToken = tokenData.RootElement.GetProperty("refresh_token").GetString();
            return (accessToken, refreshToken);
        }

        private async Task<string> RefreshAccessToken(string refreshToken)
        {
            using var client = new HttpClient();
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", spotifyInfo.ClientId },
                { "client_secret", spotifyInfo.ClientSecret }
            };
            var content = new FormUrlEncodedContent(requestBody);
            var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Fehler beim Token-Refresh: {response.StatusCode}, Details: {errorContent}");
            }
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonDocument.Parse(json);
            var newAccessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            if (tokenData.RootElement.TryGetProperty("refresh_token", out var newRefreshToken))
            {
                await SecureStorage.SetAsync("refresh_token", newRefreshToken.GetString());
            }
            return newAccessToken;
        }

        // Singleton HttpClient (wie zuvor empfohlen)
        public static class SpotifyApiClient
        {
            private static readonly HttpClient _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            public static async Task<HttpResponseMessage> ExecuteWithTokenRefresh(
                Func<HttpClient, Task<HttpResponseMessage>> apiCall,
                string accessToken,
                Func<string, Task<string>> refreshTokenFunc)
            {
                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Trim());

                var response = await apiCall(_client);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await SecureStorage.GetAsync("refresh_token");
                    if (string.IsNullOrEmpty(refreshToken))
                        throw new Exception("Refresh Token nicht verfügbar.");
                    var newAccessToken = await refreshTokenFunc(refreshToken);
                    _client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken.Trim());
                    response = await apiCall(_client);
                }

                return response;
            }
        }




        //[RelayCommand]
        //public async Task Export_list_Spotify_Clicked()
        //{
        //    Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
        //    if (Lists == null || Lists.Count == 0 || Header == null)
        //    {
        //        IsBusy = false;
        //        return;
        //    }
        //    IsBusy = true;

        //    Header header = Header_SelectedItem;
        //    header.Positions = Lists.ToList();

        //    // ChatGPT Spotify            
        //    var authUrl = "https://accounts.spotify.com/authorize" +
        //      $"?client_id={spotifyInfo.ClientId}" +
        //      "&response_type=code" +
        //      "&redirect_uri=myapp://callback" +
        //      "&scope=user-read-private%20user-read-email%20playlist-modify-private%20playlist-modify-public";

        //    var result = await WebAuthenticator.Default.AuthenticateAsync(
        //        new Uri(authUrl),
        //        new Uri("myapp://callback")
        //    );

        //    //var accessToken = result.Properties["code"];
        //    //await CreatePlaylistOnSpotify(accessToken);


        //    // Hole den Authorization Code
        //    var code = result.Properties["code"];

        //    // Tausche den Code gegen einen Access Token
        //    var accessToken = await GetAccessTokenFromCode(
        //        code,
        //        spotifyInfo.ClientId,
        //        spotifyInfo.ClientSecret, // Stelle sicher, dass ClientSecret verfügbar ist
        //        "myapp://callback"
        //    );

        //    // Rufe die Methode mit dem korrekten Access Token auf
        //    var playlistId = await CreatePlaylistOnSpotify(accessToken);

        //    if (playlistId == null)
        //    {
        //        await Application.Current.MainPage.DisplayAlert("Error", "Playlist konnte nicht erstellt werden.", "OK");
        //        return;
        //    }

        //    // Füge Songs zur Playlist hinzu
        //    // Hier solltest du die Spotify URIs deiner Songs sammeln
        //    var trackUris = header.Positions
        //        .Where(p => !p.Title.StartsWith("_")) // Filtere Beschreibungen aus
        //        .Select(p => p.Title) // Hier solltest du die Logik anpassen, um die korrekten Spotify URIs zu erhalten
        //        .ToList();

        //    // var trackUris = new List<string>();
        //    foreach (var track in trackUris)
        //    {
        //        var searchResponse = await m_client.GetAsync($"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(track)}&type=track&limit=1");
        //        if (searchResponse.IsSuccessStatusCode)
        //        {
        //            var searchJson = await searchResponse.Content.ReadAsStringAsync();
        //            var searchResult = System.Text.Json.JsonDocument.Parse(searchJson);
        //            var trackId = searchResult.RootElement.GetProperty("tracks").GetProperty("items")[0].GetProperty("id").GetString();
        //            trackUris.Add($"spotify:track:{trackId}");
        //        }
        //        else
        //        {
        //            var refreshToken = await SecureStorage.GetAsync("refresh_token");
        //            accessToken = await RefreshAccessToken(refreshToken);

        //            using var client = new HttpClient();
        //            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        //            searchResponse = await client.GetAsync($"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(track)}&type=track&limit=1");
        //        }
        //        await Task.Delay(500); // Rate-Limit respektieren
        //    }
        //    var tracksContent = new StringContent(
        //        System.Text.Json.JsonSerializer.Serialize(new { uris = trackUris }),
        //        System.Text.Encoding.UTF8,
        //        "application/json"
        //    );
        //    await m_client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", tracksContent);


        //    await Launcher.OpenAsync($"https://open.spotify.com/playlist/{playlistId}");

        //    IsBusy = false;
        //}

        //// await _spotifyService.AuthenticateAsync();

        //// File export
        ////var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        ////var filename = $"spotify_list_{timestamp}.txt";
        ////var success = await _fileExportService.ExportToDownloadsAsync(filename, header.Positions.Where(s => !s.Title.StartsWith("_")).Select(x=>x.Title));


        ////var settings = new JsonSerializerSettings();
        ////settings.Converters.Add(new OnlyPositionsJsonConverter());
        ////var json = JsonConvert.SerializeObject(header, settings);

        ////var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", json);

        ////await Utilities.ShareTextAsync(send_text);




        //async Task<string> RefreshAccessToken(string refreshToken)
        //{
        //    using var client = new HttpClient();
        //    var requestBody = new Dictionary<string, string>
        //    {
        //        { "grant_type", "refresh_token" },
        //        { "refresh_token", refreshToken },
        //        { "client_id", spotifyInfo.ClientId },
        //        { "client_secret", spotifyInfo.ClientSecret }
        //    };
        //    var content = new FormUrlEncodedContent(requestBody);
        //    var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        var errorContent = await response.Content.ReadAsStringAsync();
        //        throw new HttpRequestException($"Fehler beim Token-Refresh: {response.StatusCode}, Details: {errorContent}");
        //    }
        //    var json = await response.Content.ReadAsStringAsync();
        //    var tokenData = System.Text.Json.JsonDocument.Parse(json);
        //    return tokenData.RootElement.GetProperty("access_token").GetString();
        //}

        private async Task AddSongsToPlaylistFromAlbum(string accessToken, string albumId, string playlistFilePath)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentException("Access token is empty or null.");
            if (string.IsNullOrEmpty(albumId))
                throw new ArgumentException("Album ID is empty or zero.");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                // Benutzer-ID abrufen
                var userResponse = await client.GetAsync("https://api.spotify.com/v1/me");
                if (!userResponse.IsSuccessStatusCode)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    if (userResponse.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                        errorContent.Contains("user may not be registered"))
                    {
                        throw new UnauthorizedAccessException(
                            "Access denied: User not registered in the Spotify Developer Dashboard. " +
                            "Go to https://developer.spotify.com/dashboard > Users and Access and add the user..");
                    }
                    throw new HttpRequestException($"Fehler beim Abrufen der Benutzer-ID: {userResponse.StatusCode}, Details: {errorContent}");
                }
                var userContent = await userResponse.Content.ReadAsStringAsync();
                var user = System.Text.Json.JsonDocument.Parse(userContent);
                var userId = user.RootElement.GetProperty("id").GetString();

                // Tracks des Albums abrufen
                var albumResponse = await client.GetAsync($"https://api.spotify.com/v1/albums/{albumId}/tracks");
                if (!albumResponse.IsSuccessStatusCode)
                {
                    var errorContent = await albumResponse.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error retrieving album tracks: {albumResponse.StatusCode}, Details: {errorContent}");
                }
                var albumJson = await albumResponse.Content.ReadAsStringAsync();
                var albumTracks = System.Text.Json.JsonDocument.Parse(albumJson);
                var trackUris = albumTracks.RootElement.GetProperty("items")
                    .EnumerateArray()
                    .Select(item => item.GetProperty("uri").GetString())
                    .ToList();

                // Playlist erstellen
                var newPlaylist = new
                {
                    name = "My imported playlist",
                    description = "A celebratory music selection for a 60-year-old man's birthday, featuring classic rock, pop, and hits from his youth.",
                    @public = false
                };
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(newPlaylist),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                var playlistResponse = await client.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content);
                if (!playlistResponse.IsSuccessStatusCode)
                {
                    var errorContent = await playlistResponse.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error creating playlist: {playlistResponse.StatusCode}, Details: {errorContent}");
                }
                var playlistJson = await playlistResponse.Content.ReadAsStringAsync();
                var playlist = System.Text.Json.JsonDocument.Parse(playlistJson);
                var playlistId = playlist.RootElement.GetProperty("id").GetString();

                // Album-Tracks zur Playlist hinzufügen
                var tracksContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { uris = trackUris }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                await client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", tracksContent);

                // Zusätzliche Tracks aus playlist.txt hinzufügen
                if (!string.IsNullOrEmpty(playlistFilePath) && File.Exists(playlistFilePath))
                {
                    var additionalTracks = File.ReadAllLines(playlistFilePath);
                    var additionalTrackUris = new List<string>();
                    foreach (var track in additionalTracks)
                    {
                        var searchResponse = await client.GetAsync($"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(track)}&type=track&limit=1");
                        if (searchResponse.IsSuccessStatusCode)
                        {
                            var searchJson = await searchResponse.Content.ReadAsStringAsync();
                            var searchResult = System.Text.Json.JsonDocument.Parse(searchJson);
                            var trackId = searchResult.RootElement.GetProperty("tracks").GetProperty("items")[0].GetProperty("id").GetString();
                            additionalTrackUris.Add($"spotify:track:{trackId}");
                        }
                        await Task.Delay(200); // Rate-Limit respektieren
                    }
                    if (additionalTrackUris.Any())
                    {
                        var additionalTracksContent = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(new { uris = additionalTrackUris }),
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );
                        await client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", additionalTracksContent);
                    }
                }

                // Playlist in Spotify-App öffnen
                string spotifyUri = $"spotify:playlist:{playlistId}";
                string httpsUri = $"https://open.spotify.com/playlist/{playlistId}";
                try
                {
                    if (await Launcher.CanOpenAsync(httpsUri))
                    {
                        await Launcher.OpenAsync(httpsUri); // HTTPS bevorzugen
                    }
                    else if (await Launcher.CanOpenAsync(spotifyUri))
                    {
                        await Launcher.OpenAsync(spotifyUri);
                    }
                    else
                    {
                        throw new Exception("Spotify app or browser not available.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening playlist: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private async Task<string?> CreatePlaylistOnSpotify(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token is empty or null.");
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                // Benutzer-ID abrufen
                var userResponse = await client.GetAsync("https://api.spotify.com/v1/me");
                if (!userResponse.IsSuccessStatusCode)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error retrieving user ID: {userResponse.StatusCode}, Details: {errorContent}");
                }
                var userContent = await userResponse.Content.ReadAsStringAsync();
                var user = System.Text.Json.JsonDocument.Parse(userContent);
                var userId = user.RootElement.GetProperty("id").GetString();

                var listName = Header_SelectedItem.ListName;
                string listDescription = string.Empty;

                var listDescription_1 = Header_SelectedItem.Positions.FirstOrDefault(x => x.Title.StartsWith("_"));
                if (listDescription_1 != null && !string.IsNullOrEmpty(listDescription_1.Title) && listDescription_1.Title.StartsWith("_"))
                {
                    int ind_dp = listDescription_1.Title.IndexOf(":");
                    if (ind_dp > 0 && listDescription_1.Title.Length > ind_dp + 1)
                        listDescription = listDescription_1.Title.Substring(ind_dp + 1, listDescription_1.Title.Length - ind_dp - 1).Trim();
                }

                // Playlist erstellen
                var newPlaylist = new
                {
                    name = listName,
                    description = listDescription,
                    @public = false
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(newPlaylist),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error creating playlist: {response.StatusCode}, Details: {errorContent}");
                }

                var playlistJson = await response.Content.ReadAsStringAsync();
                var playlist = System.Text.Json.JsonDocument.Parse(playlistJson);
                var playlistId = playlist.RootElement.GetProperty("id").GetString();

                return playlistId;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Fehler bei der Spotify API: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unerwarteter Fehler: {ex.Message}");
                throw;
            }
        }


        [RelayCommand]
        public void Reset_current_list_Clicked()
        {
            int trail = 1;
        }

        #endregion


        #region FUNCTIONS

        public async Task DeleteHeaderInSupabase(Header header)
        {
            if (header == null || _supabaseService == null)
                return;
            await _supabaseService.DeleteHeaderWithDetailsAsync(header);
        }


        public async Task LoadHeaders()
        {
            Headers?.Clear();
            FilteredList?.Clear();

            var headers = await _databaseService.GetHeadersAsync();
            if (headers == null)
                return;

            Headers = new ObservableCollection<Header>(headers);
            FilteredList = new ObservableCollection<Header>(Headers);
        }

        //public async Task LoadHeaders()
        //{
        //    var headers = await _databaseService.GetHeadersAsync();
        //    if (headers == null)
        //        return;

        //    // Bestehende Collections aktualisieren
        //    Headers?.Clear();
        //    FilteredList?.Clear();

        //    foreach (var header in headers)
        //    {
        //        Headers?.Add(header);
        //        FilteredList?.Add(header);
        //    }
        //}

        public async Task<Header> AddHeader(string HeaderEntryText, bool IsSynchronized, string? Id = null)
        {
            IsBusy = true;

            Header_SelectedItem = null;
            Header newHeader = new Header();
            //if (!string.IsNullOrEmpty(Id))
            newHeader = new Header { ListName = HeaderEntryText, UpdatedAt = DateTime.Now.ToUniversalTime(), IsSynchronized = IsSynchronized };
            //else
            //    newHeader = new Header { Id = Guid.NewGuid().ToString(), ListName = HeaderEntryText, UpdatedAt = DateTime.Now };

            var new_id = await _databaseService.AddHeaderAsync(newHeader, Id);
            newHeader.Id = new_id;

            await LoadHeaders();

            Header_SelectedItem = newHeader;

            IsBusy = false;

            return newHeader;
        }

        public async Task LoadLists()
        {
            IsBusy = true;

            Lists.Clear();
            Lists_undone.Clear();
            Lists_done.Clear();
            Label_Lists = Properties.Resources.Lists.ToUpper() + " (0)";

            Label_done = string.Format("{0} ({1})", Properties.Resources.done, Lists_done.Count);
            Label_undone = string.Format("{0} ({1})", Properties.Resources.undone, Lists_undone.Count);

            if (Header_SelectedItem == null)
                return;

            PositionListViewVisible = false;

            var _pos_arr = await _databaseService.GetPositionsAsync(Header_SelectedItem.Id);
            if (_pos_arr == null || _pos_arr.Count == 0)
            {
                IsBusy = false;
                return;
            }
            Lists = _pos_arr.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToObservableCollection();
            Lists_undone = _pos_arr.Where(a => a.IsCompleted == false).OrderBy(x => x.Title).ToObservableCollection();

            if (SelectedItem_KindOfSorting == null)
            {
                // ????????
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            }

            if (SelectedItem_KindOfSorting.Value == "date")
                Lists_done = _pos_arr.Where(a => a.IsCompleted).OrderByDescending(x => x.UpdatedAt).ToObservableCollection();
            else if (SelectedItem_KindOfSorting.Value == "alpha")
                Lists_done = _pos_arr.Where(a => a.IsCompleted).OrderBy(x => x.Title).ToObservableCollection();

            Label_done = string.Format("{0} ({1})", Properties.Resources.done, Lists_done.Count);
            Label_undone = string.Format("{0} ({1})", Properties.Resources.undone, Lists_undone.Count);
            Label_Lists = string.Format(Properties.Resources.Lists.ToUpper() + " ({0})", Lists_done.Count + Lists_undone.Count);

            IsBusy = false;
            PositionListViewVisible = true;
        }

        public async Task AddPosition(Position position, bool generate_new_guid, bool Replace_old_word_when_inserting)
        {
            if (Header_SelectedItem == null || position == null)
                return;

            IsBusy = true;

            if (Replace_old_word_when_inserting && position != null && position.Title != null)
                await DeleteIfAvailable(position.Title);


            await _databaseService.AddPositionAsync(position, generate_new_guid);  // newPosition);
            await LoadLists();

            //Lists.Clear();
            var sort_pos_arr = Lists.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            Lists = new ObservableCollection<Position>(sort_pos_arr);

            IsBusy = false;
        }

        private async Task DeleteIfAvailable(string positionEntryText)
        {
            if (Lists == null || Lists.Count == 0)
                return;

            var first_word = GetFirstWordFromString(positionEntryText);
            if (first_word == null) return;

            var pos = Lists.FirstOrDefault(x => x.Title != null && x.Title.Contains(first_word, StringComparison.OrdinalIgnoreCase));
            if (pos != null)
            {
                if (!pos.IsCompleted)  // ??????????
                    await DeletePosition(pos);
            }
        }

        private string? GetFirstWordFromString(string positionEntryText)
        {
            if (!string.IsNullOrEmpty(positionEntryText))
            {
                var arr = positionEntryText.Split(' ');
                if (arr != null && arr.Length > 0)
                    return arr[0];
                else
                    return null;
            }
            return null;
        }

        public async Task DeleteHeader(Header header)
        {
            if (header == null) return;

            IsBusy = true;

            Header_SelectedItem = header;
            await DeleteListsByHeaderIdAsync();
            await _databaseService.DeleteHeaderAsync(header.Id);
            await LoadHeaders();
            await LoadLists();

            IsBusy = false;
        }

        public async Task UpdatePosition(Position pos)
        {
            IsBusy = true;

            Position_selectedItem = pos;
            pos.UpdatedAt = DateTime.Now.ToUniversalTime();
            await _databaseService.UpdatePositionAsync(pos);

            await LoadLists();

            IsBusy = false;
        }

        public async Task DeletePosition(Models.Position pos)
        {
            Position_selectedItem = pos;
            await _databaseService.DeletePositionAsync(pos.Id);
            await LoadLists();
        }

        public async Task DeleteListsByHeaderIdAsync()
        {
            if (Header_SelectedItem == null) return;

            await _databaseService.DeletePositionsByHeaderIdAsync(Header_SelectedItem.Id);
            await LoadLists();
        }

        public async Task<string?> CopyHeader(Header header, string new_list_name)
        {
            var Lists = await _databaseService.GetPositionsAsync(header.Id);

            Header new_header = new Header();
            new_header.UpdatedAt = DateTime.Now.ToUniversalTime();
            new_header.ListName = new_list_name;
            var new_header_id = await _databaseService.AddHeaderAsync(new_header, null);
            if (new_header_id != Guid.Empty.ToString() && Lists != null && Lists.Count > 0)
            {
                foreach (var pos in Lists)
                {
                    Position new_pos = new();
                    new_pos.HeaderId = new_header_id;
                    new_pos.Title = pos.Title;
                    new_pos.IsCompleted = false;
                    await _databaseService.AddPositionAsync(new_pos, false);
                }

                await LoadHeaders();

                var selectedItem = await _databaseService.GetHeaderAsync(new_header_id);
                if (selectedItem != null)
                    Header_SelectedItem = selectedItem;

                return new_header_id;
            }
            return null;
        }

        internal async Task EditNameHeader(Header header, string new_list_name)
        {
            Header_SelectedItem = header;
            await _databaseService.EditHeadersTitleAsync(header, new_list_name);
            await LoadHeaders();
            SetFirstItemForHeaders();
        }

        internal async Task EditIsSynchronizedHeader(Header header, bool new_IsSynchronized)
        {
            Header_SelectedItem = header;
            await _databaseService.EditHeadersIsSynchronizedAsync(header, new_IsSynchronized);
            await LoadHeaders();
            SetFirstItemForHeaders();
        }

        internal async Task EditTitleOfPosition(Position position, string title)
        {
            Position_selectedItem = position;
            await _databaseService.EditPositionsTitleAsync(position, title);
            await LoadLists();
        }

        internal async Task Sync_deleteClicked()
        {
            if (Header_SelectedItem == null) return;

            IsBusy = true;

            // Delete linkd to Supabase 
            await DeleteHeaderInSupabase(Header_SelectedItem);

            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.Operation_successfully_completed,
                visualOptions: new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                    TextColor = Colors.White
                },
                duration: TimeSpan.FromSeconds(2));

            await _databaseService.UpdateIsSynchronizedHeaderAsync(Header_SelectedItem.Id, false);

            await LoadHeaders();

            IsBusy = false;
        }




        #endregion
    }
}
