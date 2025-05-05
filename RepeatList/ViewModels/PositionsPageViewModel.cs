using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Position = RepeatList.Models.Position;

namespace RepeatList.ViewModels
{
    public partial class PositionsPageViewModel : ObservableObject   // INotifyPropertyChanged,
    {
        private DatabaseService _databaseService;
        public SupabaseService _supabaseService;

        //public event PropertyChangedEventHandler PropertyChanged;

        private SetupPageViewModel? setupPageViewModel;

        public string SelectedItem_KindOfSorting_key_name = "SelectedItem_KindOfSorting";
        public double ButtonsSize = 25;

        [ObservableProperty] public string collapse_undone_icon;
        [ObservableProperty] public string collapse_done_icon;


        [ObservableProperty] public string menu_icon = "menu.png";
        [ObservableProperty] public bool duplicate_entries_add;
        [ObservableProperty] public bool sortImages_visible = true;

        [ObservableProperty] public double imageButton_resetPositions_SizeRequest = 35;
        [ObservableProperty] public double image_sort_SizeRequest = 20;


        [ObservableProperty] public double gridheight_undone = -1;
        [ObservableProperty] public double gridheight_done = -1;

        [ObservableProperty]
        public RowDefinitionCollection rowDefinitions = new RowDefinitionCollection
        {
                //new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            };
        [ObservableProperty] public bool isvisible_undone = true;
        [ObservableProperty] public bool isvisible_done = true;

        [ObservableProperty] public bool? collapse_undone = null;
        partial void OnCollapse_undoneChanged(bool? oldValue, bool? newValue)
        {
            SetCollapseUndone(newValue);
        }

        private void SetCollapseUndone(bool? newValue)
        {
            if (newValue == null)
            {
                Collapse_undone_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "expand_icon_white.png" : "expand_icon_black.png";
                Collapse_done_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "expand_icon_white.png" : "expand_icon_black.png";

                RowDefinitions = new RowDefinitionCollection
                {
                    //new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                };
                Gridheight_undone =-1;
                Gridheight_done =-1;

                SortImages_visible=true;
                ImageButton_resetPositions_SizeRequest = 35;
                Image_sort_SizeRequest=20;
                Isvisible_undone = true;
                Isvisible_done=true;
            }
            else
            {
                if (newValue == true)
                {
                    Collapse_undone_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "expand_icon_white.png" : "expand_icon_black.png";
                    Collapse_done_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "collapse_icon_white.png" : "collapse_icon_black.png";

                    RowDefinitions = new RowDefinitionCollection
                    {
                        //new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(0), //new RowDefinition(GridLength.Star),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Star),
                        new RowDefinition(GridLength.Auto)
                    };

                    Gridheight_undone =0;

                    Gridheight_done =-1;
                    SortImages_visible=true;
                    ImageButton_resetPositions_SizeRequest = 35;
                    Image_sort_SizeRequest=20;

                    Isvisible_undone = false;
                    Isvisible_done=true;
                }
                else
                {
                    Collapse_undone_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "collapse_icon_white.png" : "collapse_icon_black.png";
                    Collapse_done_icon = Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark ? "expand_icon_white.png" : "expand_icon_black.png";

                    RowDefinitions = new RowDefinitionCollection
                    {
                        //new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Star),
                        new RowDefinition(0),  //new RowDefinition(GridLength.Auto),
                        new RowDefinition(0),  //new RowDefinition(GridLength.Star),
                        new RowDefinition(GridLength.Auto)
                    };
                    Gridheight_undone =-1;

                    Gridheight_done =0;
                    SortImages_visible=false;
                    ImageButton_resetPositions_SizeRequest = 0;
                    Image_sort_SizeRequest=0;

                    Isvisible_undone = true;
                    Isvisible_done=false;
                }
            }
        }

        
        partial void OnDuplicate_entries_addChanged(bool oldValue, bool newValue)
        {
            Menu_icon = newValue ? "menu.png" : "menu_alert.png";

            Preferences.Set("Duplicate_entries_add", newValue);
        }

        //[ObservableProperty] public bool changeListsCheckedState;
        //partial void OnChangeListsCheckedStateChanged(bool oldValue, bool newValue)
        //{
        //    string image_source = "";
        //    if (Application.Current.UserAppTheme == AppTheme.Dark)
        //    {
        //        image_source = newValue ? "check_box_check_white.png" : "check_box_blank_white.png";
        //    }
        //    else
        //    {
        //        image_source = newValue ? "check_box_check.png" : "check_box_blank.png";
        //    }

        //    PositionImageSource = image_source;
        //}




        [ObservableProperty] private string search = Properties.Resources.search;
        [ObservableProperty] private string title;  // = Properties.Resources.Positions.ToUpper();
        [ObservableProperty] public string resetImageSource;

        public PositionsPageViewModel()
        {
            //_databaseService =  new DatabaseService();
            //_supabaseService =  new SupabaseService();

            //setupPageViewModel =new SetupPageViewModel();

            //_ = setupPageViewModel.Load();
            //CurrentCulture= setupPageViewModel.SelectedItem.DefaultLanguage;

            ////_ = LoadHeaders();

            //_ = LoadPositions();

            ////SetFirstItemForHeaders();
            //InitSelectedItem_KindOfSorting();
            ////SetResetImageSource();
        }

        public PositionsPageViewModel(Header selectedItem)
        {
            _databaseService = new DatabaseService();
            _supabaseService = new SupabaseService();

            setupPageViewModel = new SetupPageViewModel();
            _ = setupPageViewModel.Load();
            CurrentCulture = setupPageViewModel.SelectedItem.DefaultLanguage;

            if (selectedItem.ListName != null)
                Header_SelectedItem = selectedItem;

            _ = LoadPositions();

            //SetFirstItemForHeaders();
            InitSelectedItem_KindOfSorting();

            SetCollapseUndone(null);
        }

        public void InitLabels()
        {
            Title = Header_SelectedItem.ListName;
            Search = Properties.Resources.search;
            Label_Positions = Properties.Resources.Positions.ToUpper() + " (0)";
            No_items_to_display = Properties.Resources.No_items_to_display;
            Label_lists = Properties.Resources.Lists.ToUpper();
            Label_addNewList = Properties.Resources.AddNewList;
            Label_AddNewItem = Properties.Resources.AddNewItem;
            Label_ResetPositions = Properties.Resources.ResetPositions;
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
            if (Headers != null && Headers.Count > 0)
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


        [ObservableProperty] public string no_items_to_display = Properties.Resources.No_items_to_display;
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
        [ObservableProperty] public bool changePositionsCheckedState;
        partial void OnChangePositionsCheckedStateChanged(bool oldValue, bool newValue)
        {
            string image_source = "";
            if (Application.Current != null && Application.Current.UserAppTheme == AppTheme.Dark)
            {
                image_source = newValue ? "check_box_check_white.png" : "check_box_blank_white.png";
            }
            else
            {
                image_source = newValue ? "check_box_check.png" : "check_box_blank.png";
            }

            PositionImageSource = image_source;
        }


        [ObservableProperty] private string currentCulture;
        [ObservableProperty] private int imageButton_size = 30;
        [ObservableProperty] private string label_lists = Properties.Resources.Lists.ToUpper();
        [ObservableProperty] private string label_addNewList = Properties.Resources.AddNewList;
        [ObservableProperty] private string label_Positions = Properties.Resources.Positions.ToUpper() + " (0)";
        [ObservableProperty] private string label_AddNewItem = Properties.Resources.AddNewItem;
        [ObservableProperty] private string label_ResetPositions = Properties.Resources.ResetPositions;
        [ObservableProperty] private string _label_Export_list = Properties.Resources.Export_list;
        [ObservableProperty] private string label_copy_list_to_clipboard = Properties.Resources.Copy_list_to_clipboard;
        [ObservableProperty] private string _label_Reset_current_list = Properties.Resources.Reset_current_list;
        [ObservableProperty] private string _label_done = Properties.Resources.done;
        [ObservableProperty] private string _label_undone = Properties.Resources.undone;
        [ObservableProperty] private string label_paste_from_clipboard = Properties.Resources.Paste_from_clipboard;

        [ObservableProperty] private static Header header_SelectedItem;
        [ObservableProperty] private ObservableCollection<Header>? headers = new ObservableCollection<Header>();
        [ObservableProperty] private Header? header = new Header();
        [ObservableProperty] private Models.Position? position_selectedItem;

        [ObservableProperty] private ObservableCollection<Position> positions = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_undone = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_done = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_undone_filterd = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_done_filtered = new ObservableCollection<Position>();

        public event PropertyChangedEventHandler _PropertyChanged;
        protected void OnPropertyChanged_(string propertyName)
        {
            _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ObservableProperty] public bool isBusy;
        [ObservableProperty] private string expander_listsIcon = "collapse_icon.png";
        [ObservableProperty] private string expander_positionsIcon = "collapse_icon.png";

        [ObservableProperty] public bool isExpander_listsExpended = false;
        [ObservableProperty] public bool isExpander_positionsExpended = true;

        [ObservableProperty] public double positionsListHeight;

        public double IconsHeightRequested = 30;

        [ObservableProperty] private string inputText;
        [ObservableProperty] private string inputText_placeholder;
        [ObservableProperty] ObservableCollection<Position> filteredList = new();

        partial void OnIsExpander_listsExpendedChanged(bool oldValue, bool newValue)
        {
            Expander_listsIcon = newValue ? "collapse_icon.png" : "expand_icon.png";
            PositionsListHeight = newValue ? 400 : 600;
        }

        partial void OnIsExpander_positionsExpendedChanged(bool oldValue, bool newValue)
        {
            Expander_positionsIcon = newValue ? "collapse_icon.png" : "expand_icon.png";
        }

        #endregion


        #region COMMANDS       

        [RelayCommand]
        public void Undone_ImageButtonClicked()
        {
            Collapse_undone = Collapse_undone == null ? false : null;
        }

        [RelayCommand]
        public void Done_ImageButtonClicked()
        {
            Collapse_undone = Collapse_undone == null ? true : null;
        }

        [RelayCommand]
        public async Task Export_list_textClicked()
        {
            IsBusy = true;

            string send_text = Header_SelectedItem.ListName + ": " + Environment.NewLine;
            for (int i = 0; i < Positions_undone.Count; i++)
            {
                send_text += (i + 1) + ". " + Positions_undone[i].Title + Environment.NewLine;
            }
            await Utilities.ShareTextAsync(send_text);

            IsBusy = false;
        }

        [RelayCommand]
        public async Task Import_listClicked()
        {
            Guid tmp_guid = Guid.Empty;

            if(Application.Current == null)
                return;

            string guid_str = await Application.Current.MainPage.DisplayPromptAsync(
                 Properties.Resources.import_liste,
                 Properties.Resources.Please_enter_the_ID_of_the_list_to_be_synchronised);
            if (!string.IsNullOrEmpty(guid_str))
            {
                if (!Guid.TryParse(guid_str, out tmp_guid))
                {
                    await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_List_ID,
                        visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb(Constantes.Color_Error_string), TextColor = Colors.White },
                duration: TimeSpan.FromSeconds(2));
                    IsBusy = false;
                    return;
                }
            }
            else
                return;

            IsBusy = true;

            (Header Header, List<Position> Positions) sync_responce = await _supabaseService.GetHeaderWithPositionsByIdAsync(tmp_guid);

            if (sync_responce.Header == null || sync_responce.Positions == null)
            {
                await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_not_available_or_corrupt,
                    visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb(Constantes.Color_Error_string),
                        TextColor = Colors.White
                    },
                duration: TimeSpan.FromSeconds(2));
                IsBusy = false;
                return;
            }

            var _header = Headers != null ? Headers.FirstOrDefault(x => x.Id == sync_responce.Header.Id) : null;
            if (_header != null)
                Header = sync_responce.Header;
            else
                Header = await AddHeader(sync_responce.Header.ListName, sync_responce.Header.Id);

            foreach (var pos in sync_responce.Positions)
            {
                await AddPosition(pos, false);
            }

            Header_SelectedItem = Header;
            await EditIsSynchronizedHeader(Header_SelectedItem, true);

            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.List_was_imported_successfully,
                visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb(Constantes.Color_Success_string), TextColor = Colors.White }
                , duration: TimeSpan.FromSeconds(2));

            IsBusy = false;
        }

        [RelayCommand]
        public async Task Sync_list_downClicked(Header header)
        {
            Guid tmp_guid = new Guid(header.Id);

            try
            {
                //IsBusy = true;

                (Header header, List<Position> position) sync_responce = await _supabaseService.GetHeaderWithPositionsByIdAsync(tmp_guid);

                if (sync_responce.header == null || sync_responce.position == null)
                {
                    string guid_str = await Application.Current.MainPage.DisplayPromptAsync(
                      Properties.Resources.Would_you_like_to_work_with_someone_on_a_current_list,
                      Properties.Resources.Please_enter_the_ID_of_the_list_to_be_synchronised);
                    if (!string.IsNullOrEmpty(guid_str))
                    {
                        if (!Guid.TryParse(guid_str, out tmp_guid))
                        {
                            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.String_is_not_a_valid_List_ID,
                                visualOptions: new SnackbarOptions { BackgroundColor = Color.FromArgb(Constantes.Color_Error_string), TextColor = Colors.White }
                                , duration: TimeSpan.FromSeconds(2));
                            IsBusy = false;
                            return;
                        }
                    }
                }

                if (sync_responce.header == null || sync_responce.header.Id == null)
                {
                    return;
                }

                //var _header = Headers.FirstOrDefault(x => x.Id == sync_responce.header.Id);
                //if (_header != null)
                //{
                foreach (var new_pos in sync_responce.position)
                {
                    var old_pos = Positions.FirstOrDefault(p => p.Id == new_pos.Id);
                    if (old_pos == null)
                    {
                        await AddPosition(new_pos, false);
                    }
                    else
                    {
                        DateTime supaBaseDateTime = ((DateTime)new_pos.UpdatedAt);
                        DateTime localDateTime = TimeZoneInfo.ConvertTimeToUtc((DateTime)old_pos.UpdatedAt);

                        if (supaBaseDateTime > localDateTime)
                            await UpdatePosition(new_pos);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //IsBusy = false;
            }
        }


        [RelayCommand]
        public async Task Sync_list_upClicked()
        {
            if (Positions == null || Positions.Count == 0 || Header == null)
                return;

            IsBusy = true;

            await _supabaseService.SyncHeaderWithDetailsAsync(Header_SelectedItem);

            await EditIsSynchronizedHeader(Header_SelectedItem, true);

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
        public async Task Export_list_Clicked()
        {
            if (Positions == null || Positions.Count == 0 || Header == null)
                return;
            IsBusy = true;

            Header header = Header_SelectedItem;
            header.Positions = Positions.ToList();

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(header);
            await Utilities.ShareTextAsync(json);

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
            if (header == null) return;
            await _supabaseService.DeleteHeaderWithDetailsAsync(header);
        }


        public async Task LoadHeaders()
        {
            Headers = null;

            var headers = await _databaseService.GetHeadersAsync();
            if (headers == null)
                return;

            Headers = new ObservableCollection<Header>(headers);
        }

        public async Task<Header> AddHeader(string HeaderEntryText, string? Id = null)
        {
            Header_SelectedItem = null;
            Header newHeader = new Header();
            //if (!string.IsNullOrEmpty(Id))
            newHeader = new Header { ListName = HeaderEntryText, UpdatedAt = DateTime.Now.ToUniversalTime() };
            //else
            //    newHeader = new Header { Id = Guid.NewGuid().ToString(), ListName = HeaderEntryText, UpdatedAt = DateTime.Now };

            var new_id = await _databaseService.AddHeaderAsync(newHeader, Id);
            newHeader.Id = new_id;

            await LoadHeaders();

            Header_SelectedItem = newHeader;

            return newHeader;
        }

        internal async Task LoadPositions()
        {
            IsBusy = true;

            Positions.Clear();
            Positions_undone.Clear();
            Positions_done.Clear();
            Positions_undone_filterd.Clear();
            Positions_done_filtered.Clear();

            Label_Positions = Properties.Resources.Positions.ToUpper() + " (0)";

            Label_done = string.Format("{0} ({1})", Properties.Resources.done, Positions_done.Count);
            Label_undone = string.Format("{0} ({1})", Properties.Resources.undone, Positions_undone.Count);

            if (Header_SelectedItem == null)
                return;

            PositionListViewVisible = false;

            var _pos_arr = await _databaseService.GetPositionsAsync(Header_SelectedItem.Id);
            if (_pos_arr == null || _pos_arr.Count == 0)
            {
                IsBusy = false;
                return;
            }
            Positions = _pos_arr.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToObservableCollection();
            Positions_undone = _pos_arr.Where(a => a.IsCompleted == false).OrderBy(x => x.Title).ToObservableCollection();

            if (SelectedItem_KindOfSorting == null)
            {
                // ????????
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            }

            if (SelectedItem_KindOfSorting.Value == "date")
                Positions_done = _pos_arr.Where(a => a.IsCompleted).OrderByDescending(x => x.UpdatedAt).ToObservableCollection();
            else if (SelectedItem_KindOfSorting.Value == "alpha")
                Positions_done = _pos_arr.Where(a => a.IsCompleted).OrderBy(x => x.Title).ToObservableCollection();

            Label_done = string.Format("{0} ({1})", Properties.Resources.done, Positions_done.Count);
            Label_undone = string.Format("{0} ({1})", Properties.Resources.undone, Positions_undone.Count);
            Label_Positions = string.Format(Properties.Resources.Positions.ToUpper() + " ({0})", Positions_done.Count + Positions_undone.Count);

            FilteredList = new ObservableCollection<Position>(_pos_arr);

            Positions_undone_filterd = new ObservableCollection<Position>(Positions_undone);
            Positions_done_filtered = new ObservableCollection<Position>(Positions_done);

            IsBusy = false;
            PositionListViewVisible = true;
        }

        public async Task AddPosition(Position position, bool generate_new_guid)
        {
            if (Header_SelectedItem == null || position == null) return;

            if (Duplicate_entries_add == false)  // old positins are deleted
            {
                //if (!string.IsNullOrEmpty(position.Title))
                //{
                //    bool answer = await Application.Current.MainPage.DisplayAlert(
                //            Properties.Resources.Are_you_sure,
                //            Properties.Resources.Old_entries_that_begin_with_the_keyword, Properties.Resources.yes, Properties.Resources.no);
                //    if (answer)
                //    {
                // Delete all positions with the same first word
                await DeleteIfAvailable(position.Title);
                //}
                //}
            }

            var new_guid = await _databaseService.AddPositionAsync(position, generate_new_guid);
            position.Id = new_guid;

            await LoadPositions();

            //Positions.Clear();
            var sort_pos_arr = Positions.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            Positions = new ObservableCollection<Position>(sort_pos_arr);

            if (Header_SelectedItem.IsSynchronized)
            {
                await _supabaseService.SyncPositionAsync(position);
            }

            IsBusy = false;
        }

        private async Task DeleteIfAvailable(string positionEntryText)
        {
            var first_word = GetFirstWordFromString(positionEntryText);

            // First word of the title matches  
            var pos_arr = Positions
                .Where(x => x.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?.Equals(first_word, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            foreach (var pos in pos_arr)
            {
                if (pos != null)
                {
                    //if (!pos.IsCompleted)  // ??????????
                    await DeletePosition(pos);
                }
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
            await DeletePositionsByHeaderIdAsync();
            await _databaseService.DeleteHeaderAsync(header.Id);
            await LoadHeaders();
            await LoadPositions();

            IsBusy = false;
        }

        public async Task UpdatePosition(Position pos)
        {
            IsBusy = true;

            pos.UpdatedAt = DateTime.Now.ToUniversalTime();

            if (Header_SelectedItem.IsSynchronized)
            {
                await _supabaseService.SyncPositionAsync(pos);
            }

            Position_selectedItem = pos;

            await _databaseService.UpdatePositionAsync(pos);

            await LoadPositions();

            IsBusy = false;
        }

        public async Task DeletePosition(Models.Position pos)
        {
            Position_selectedItem = pos;
            await _databaseService.DeletePositionAsync(pos.Id);

            if (Header_SelectedItem.IsSynchronized)
                await _supabaseService.DeletePositionAsync(pos);

            await LoadPositions();
        }

        public async Task DeletePositionsByHeaderIdAsync()
        {
            if (Header_SelectedItem == null) return;

            await _databaseService.DeletePositionsByHeaderIdAsync(Header_SelectedItem.Id);
            await LoadPositions();
        }

        public async Task<string?> CopyHeader(Header header, string new_list_name)
        {
            var positions = await _databaseService.GetPositionsAsync(header.Id);

            Header new_header = new Header();
            new_header.UpdatedAt = DateTime.Now.ToUniversalTime();
            new_header.ListName = new_list_name;
            var new_header_id = await _databaseService.AddHeaderAsync(new_header, null);
            if (new_header_id != Guid.Empty.ToString() && positions != null && positions.Count > 0)
            {
                foreach (var pos in positions)
                {
                    Position new_pos = new();
                    new_pos.HeaderId = new_header_id;
                    new_pos.Title = pos.Title;
                    new_pos.IsCompleted = false;
                    await _databaseService.AddPositionAsync(new_pos, false);
                }

                await LoadHeaders();

                var selectedItem = await _databaseService.GetHeaderAsync(new_header_id);
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

            if (Header_SelectedItem.IsSynchronized)
                await _supabaseService.SyncPositionAsync(position);

            await LoadPositions();
        }

        public async Task ResetPositionsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            if (Header_SelectedItem == null) return;

            await _databaseService.UpdateIsCompletedPositionsAsync(Header_SelectedItem.Id, false);

            if (Header_SelectedItem.IsSynchronized)
            {
                Header_SelectedItem.Positions = await _databaseService.GetPositionsAsync(Header_SelectedItem.Id);

                foreach (var pos in Header_SelectedItem.Positions)
                {
                    pos.IsCompleted = false;
                    pos.UpdatedAt = DateTime.Now.ToUniversalTime();
                    await _supabaseService.SyncPositionAsync(pos);
                }
            }

            await LoadPositions();

            IsBusy = false;
        }


        #endregion
    }
}
