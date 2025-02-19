using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Position = RepeatList.Models.Position;
using System.Text.Json.Serialization;

namespace RepeatList.ViewModels
{
    public partial class MainPageViewModel : ObservableObject   // INotifyPropertyChanged,
    {
        private DatabaseService _databaseService;
        public event PropertyChangedEventHandler PropertyChanged;
        private SetupPageViewModel? setupPageViewModel;
        public string SelectedItem_KindOfSorting_key_name = "SelectedItem_KindOfSorting";

        public double ButtonsSize = 30;

        public MainPageViewModel()
        {
            _databaseService =  new DatabaseService();
            setupPageViewModel=new SetupPageViewModel();

            _ = setupPageViewModel.Load();
            CurrentCulture= setupPageViewModel.SelectedItem.DefaultLanguage;

            _ = LoadHeaders();

            SetFirstItemForHeaders();

            //KindOfSortingPicker.ItemsSource = ItemSource_KindOfSorting.ToObservableCollection();

            InitSelectedItem_KindOfSorting();
        }

        private void InitSelectedItem_KindOfSorting()
        {

            string _selectedItem_KindOfSorting = Preferences.Get(SelectedItem_KindOfSorting_key_name, "date");
            if (_selectedItem_KindOfSorting == null)
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            else
                SelectedItem_KindOfSorting =  ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting);  // new CMBType_String(Properties.Resources.sort_by, "date");
        }

        public void SetFirstItemForHeaders()
        {
            if (Headers != null && Headers.Count>0)
                Header_SelectedItem = Headers[0];
        }

        #region PROPERTIES

        bool replace_old_word_when_inserting;
        public bool Replace_old_word_when_inserting
        {
            get
            {
                replace_old_word_when_inserting =Preferences.Get("Replace_old_word_when_inserting", true);
                return replace_old_word_when_inserting;
            }
        }

        [ObservableProperty] public string title_sort_by = Properties.Resources.sort_by;
        [ObservableProperty] public string title_KindOfSorting = "Sort";
        [ObservableProperty] public CMBType_String selectedItem_KindOfSorting;
        //async void OnSelectedItem_KindOfSortingChanged(CMBType_String oldValue, CMBType_String newValue)
        //{
        //    await LoadPositions();
        //}

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
            PositionImageSource = newValue ? "check_box_check.png" : "check_box_blank.png";
        }


        [ObservableProperty] private string currentCulture;

        [ObservableProperty] private int imageButton_size = 35;
        [ObservableProperty] private string label_lists = Properties.Resources.Lists.ToUpper();
        [ObservableProperty] private string label_addNewList = Properties.Resources.AddNewList;
        [ObservableProperty] private string label_Positions = Properties.Resources.Positions.ToUpper();
        [ObservableProperty] private string label_AddNewItem = Properties.Resources.AddNewItem;
        [ObservableProperty] private string label_ResetPositions = Properties.Resources.ResetPositions;
        [ObservableProperty] private string _label_Export_list = Properties.Resources.Export_list;
        [ObservableProperty] private string label_copy_list_to_clipboard = Properties.Resources.Copy_list_to_clipboard;
        [ObservableProperty] private string _label_Reset_current_list = Properties.Resources.Reset_current_list;
        [ObservableProperty] private string _label_done = Properties.Resources.done;
        [ObservableProperty] private string _label_undone = Properties.Resources.undone;
        [ObservableProperty] private string label_paste_from_clipboard = Properties.Resources.Paste_from_clipboard;

        [ObservableProperty] private Header header_SelectedItem;
        [ObservableProperty] private ObservableCollection<Header>? headers = new ObservableCollection<Header>();
        [ObservableProperty] private Header? header = new Header();
        [ObservableProperty] private Models.Position? position_selectedItem;

        [ObservableProperty] private ObservableCollection<Position> positions = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_undone = new ObservableCollection<Position>();
        [ObservableProperty] private ObservableCollection<Position> positions_done = new ObservableCollection<Position>();

        public event PropertyChangedEventHandler _PropertyChanged;
        protected void OnPropertyChanged_(string propertyName)
        {
            _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ObservableProperty] public bool isBusy;
        [ObservableProperty] private string expander_listsIcon = "collapse_icon.png";

        [ObservableProperty] public bool isExpander_listsExpended = true;

        [ObservableProperty] public double positionsListHeight;

        //[ObservableProperty] public string exportedList;
        //[ObservableProperty] public string exportedListTitle;

        partial void OnIsExpander_listsExpendedChanged(bool oldValue, bool newValue)
        {
            Expander_listsIcon=newValue ? "collapse_icon.png" : "expand_icon.png";
            PositionsListHeight=newValue ? 400 : 600;
        }

        #endregion


        #region COMMANDS       

        [RelayCommand]
        public async Task Export_list_Clicked() //string ExportedList, string Title)
        {
            if (Positions == null || Positions.Count == 0 || Header==null)
                return;
            var json = JsonSerializer.Serialize(Positions);
            await Utilities.ShareTextAsync(json);

            //var ExportedList = String.Join(",,", Positions.Select(x => x.Title).ToList());
            //var ExportedListTitle = "Export: " + Header_SelectedItem.ListName;

            //var file_name = string.Format("repeat_list_export_{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"));
            //await Utilities.ShareFileAsync(file_name, ExportedList, ExportedListTitle);
        }

        [RelayCommand]
        public void Reset_current_list_Clicked()
        {
            int trail = 1;
        }

        #endregion


        #region FUNCTIONS


        public async Task LoadHeaders()
        {
            var headers = await _databaseService.GetHeadersAsync();
            if (headers == null)
                return;

            //Headers.Clear();
            Headers = new ObservableCollection<Header>(headers);
            //foreach (var pos in headers)
            //{
            //    Headers.Add(pos);
            //}
        }

        public async Task<int> AddHeader(string HeaderEntryText)
        {
            var newHeader = new Header { ListName = HeaderEntryText, Date = DateTime.Now };
            var new_id = await _databaseService.AddHeaderAsync(newHeader);

            await LoadHeaders();


            //var selectedItem = await _databaseService.GetHeaderAsync(new_id);
            //if (selectedItem != null)
            //    Header_SelectedItem = selectedItem;

            return new_id;
        }

        public async Task LoadPositions()
        {
            IsBusy = true;

            if (Header_SelectedItem == null)
                return;

            Positions.Clear();
            Positions_undone.Clear();
            Positions_done.Clear();

            PositionListViewVisible =false;

            var _pos_arr = await _databaseService.GetPositionsAsync(Header_SelectedItem.Id);
            if (_pos_arr == null || _pos_arr.Count == 0)
            {
                IsBusy = false;
                return;
            }
            //var sorted_list = _pos_arr.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            //SetSortedPositionsList(sorted_list);



            Positions = _pos_arr.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToObservableCollection();
            Positions_undone = _pos_arr.Where(a => a.IsCompleted== false).OrderBy(x => x.Title).ToObservableCollection();

            if (SelectedItem_KindOfSorting == null)
            {
                // ????????
                SelectedItem_KindOfSorting = new CMBType_String(Properties.Resources.sort_by, "date");
            }

            if (SelectedItem_KindOfSorting.Value=="date")
                Positions_done = _pos_arr.Where(a => a.IsCompleted).OrderByDescending(x => x.InsertedAt).ToObservableCollection();
            else if (SelectedItem_KindOfSorting.Value=="alpha")
                Positions_done = _pos_arr.Where(a => a.IsCompleted).OrderBy(x => x.Title).ToObservableCollection();

            Label_done = string.Format("{0} ({1})", Properties.Resources.done, Positions_done.Count);
            Label_undone = string.Format("{0} ({1})", Properties.Resources.undone, Positions_undone.Count);

            IsBusy=false;
            PositionListViewVisible=true;
        }

        public async Task AddPosition(string PositionEntryText)
        {
            if (Header_SelectedItem  == null || string.IsNullOrEmpty(PositionEntryText)) return;

            IsBusy = true;

            if (Replace_old_word_when_inserting)
                await DeleteIfAvailable(PositionEntryText);

            var newPosition = new Models.Position { HeaderId = Header_SelectedItem.Id, Title = PositionEntryText, IsCompleted = false, InsertedAt=DateTime.Now };
            await _databaseService.AddPositionAsync(newPosition);
            await LoadPositions();

            //Positions.Clear();
            var sort_pos_arr = Positions.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            Positions = new ObservableCollection<Position>(sort_pos_arr);

            //var sorted_list = Positions.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            ////SetSortedPositionsList(); // sorted_list);

            //Positions = new ObservableCollection<Position>();  //.Clear();  //Positions.Clear();
            //foreach (var item in sorted_list)  //sortedItems)
            //{
            //    Positions.Add(item);
            //}

            IsBusy = false;
        }

        private async Task DeleteIfAvailable(string positionEntryText)
        {
            var first_word = GetFirstWordFromString(positionEntryText);
            var pos = Positions.FirstOrDefault(x => x.Title.ToLower().Contains(first_word.ToLower()));
            if (pos != null)
            {
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

        public async Task DeleteHeader(Models.Header header)
        {
            if (header == null) return;

            Header_SelectedItem = header;
            await DeletePositionsByHeaderIdAsync();
            await _databaseService.DeleteHeaderAsync(header.Id);
            await LoadHeaders();
            await LoadPositions();

        }

        public async Task UpdatePosition(Models.Position pos)
        {
            IsBusy=true;

            Position_selectedItem = pos;
            pos.InsertedAt = DateTime.Now;
            await _databaseService.UpdatePositionAsync(pos);

            await LoadPositions();

            //var sorted_list = Positions.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            ////SetSortedPositionsList(sorted_list);
            //Positions.Clear();  //Positions.Clear();
            //foreach (var item in sorted_list)  //sortedItems)
            //{
            //    Positions.Add(item);
            //}

            IsBusy =false;
        }

        private void SetSortedPositionsList(List<Position> sortedItems)
        {
            var sorted_list = sortedItems.OrderBy(x => x.IsCompleted).ThenBy(a => a.Title).ToList();
            Positions.Clear();  //Positions.Clear();
            foreach (var item in sorted_list)  //sortedItems)
            {
                Positions.Add(item);
            }
        }

        public async Task DeletePosition(Models.Position pos)
        {
            Position_selectedItem= pos;
            await _databaseService.DeletePositionAsync(pos.Id);
            await LoadPositions();
        }

        public async Task DeletePositionsByHeaderIdAsync()
        {
            if (Header_SelectedItem == null || IsBusy) return;

            await _databaseService.DeletePositionsByHeaderIdAsync(Header_SelectedItem.Id);
            await LoadPositions();
        }

        public async Task<int?> CopyHeader(Header header, string new_list_name)
        {
            var positions = await _databaseService.GetPositionsAsync(header.Id);

            Header new_header = new Header();
            new_header.Date = DateTime.Now;
            new_header.ListName=new_list_name;
            var new_header_id = await _databaseService.AddHeaderAsync(new_header);
            if (new_header_id > 0 && positions != null && positions.Count > 0)
            {
                foreach (var pos in positions)
                {
                    Position new_pos = new();
                    new_pos.HeaderId = new_header_id;
                    new_pos.Title = pos.Title;
                    new_pos.IsCompleted =false;
                    await _databaseService.AddPositionAsync(new_pos);
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

        internal async Task EditTitleOfPosition(Position position, string title)
        {
            Position_selectedItem = position;
            await _databaseService.EditPositionsTitleAsync(position, title);
            await LoadPositions();
        }

        public async Task ResetPositionsAsync()
        {
            if (Header_SelectedItem == null) return;

            await _databaseService.UpdateIsCompletedPositionsAsync(Header_SelectedItem.Id, false);
            await LoadPositions();
        }


        #endregion
    }
}
