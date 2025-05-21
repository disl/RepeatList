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
using Position = RepeatList.Models.Position;

namespace RepeatList.ViewModels
{
    public partial class ListsPageViewModel : ObservableObject
    {
        private DatabaseService _databaseService;
        private SupabaseService? _supabaseService;



        //public event PropertyChangedEventHandler PropertyChanged;

        private SetupPageViewModel? setupPageViewModel;

        public string SelectedItem_KindOfSorting_key_name = "SelectedItem_KindOfSorting";
        public double ButtonsSize = 25;

        [ObservableProperty] public string resetImageSource;
        [ObservableProperty] public bool supabaseService_ready;

        public ListsPageViewModel()
        {
            _databaseService = new DatabaseService();

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

            SetFirstItemForHeaders();
            InitSelectedItem_KindOfSorting();
            SetResetImageSource();
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
        [ObservableProperty] public ObservableCollection<Header> filteredList = new();
        [ObservableProperty] public string please_create_a_first_list = Properties.Resources.Please_create_a_first_list;
        [ObservableProperty] public bool isSynchronized = false;
        [ObservableProperty] public string title_sort_by = Properties.Resources.sort_by;
        [ObservableProperty] public string title_KindOfSorting = "Sort";
        [ObservableProperty] public CMBType_String selectedItem_KindOfSorting;
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
                //var json = JsonConvert.SerializeObject(header, settings);

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
        public async Task Export_list_Clicked()
        {
            Lists = (await _databaseService.GetPositionsAsync(Header_SelectedItem.Id)).ToObservableCollection();
            if (Lists == null || Lists.Count == 0 || Header == null)
            {
                IsBusy = false;
                return;
            }
            IsBusy = true;

            Header header = Header_SelectedItem;
            header.Positions = Lists.ToList();

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new OnlyPositionsJsonConverter());
            var json = JsonConvert.SerializeObject(header, settings);

            var send_text = Properties.Resources.Please_copy_this_text_to_the_clipboard_and_import_it_via_the_hamburger_menu.Replace("%1", json);

            await Utilities.ShareTextAsync(send_text);

            IsBusy = false;
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
            Headers = null;

            var headers = await _databaseService.GetHeadersAsync();
            if (headers == null)
                return;

            Headers = new ObservableCollection<Header>(headers);
            FilteredList = new ObservableCollection<Header>(Headers);
        }

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
