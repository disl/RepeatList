using RepeatList.Models;
using RepeatList.Properties;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Position = RepeatList.Models.Position;


namespace RepeatList.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private DatabaseService _databaseService;
        public event PropertyChangedEventHandler PropertyChanged;

        public double ButtonsSize = 30;

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        private string label_lists =  Resources.Lists.ToUpper();
        public string Label_lists { get => label_lists; set => SetProperty(ref label_lists, value); } 
        
        private string label_addNewList =  Resources.AddNewList;
        public string Label_AddNewList { get => label_addNewList; set => SetProperty(ref label_addNewList, value); }
        
        private string label_Positions =  Resources.Positions.ToUpper();
        public string Label_Positions { get => label_Positions; set => SetProperty(ref label_Positions, value); }
        
        private string label_AddNewItem =  Resources.AddNewItem;
        public string Label_AddNewItem { get => label_AddNewItem; set => SetProperty(ref label_AddNewItem, value); }
        
        private string label_ResetPositions =  Resources.ResetPositions;
        public string Label_ResetPositions { get => label_ResetPositions; set => SetProperty(ref label_ResetPositions, value); }



        private Header _header_SelectedItem;
        public Header Header_SelectedItem
        {
            get => _header_SelectedItem;
            set
            {
                _header_SelectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Header_SelectedItem)));
            }
        }

        private ObservableCollection<Header>? _headers = new ObservableCollection<Header>();
        public ObservableCollection<Header>? Headers
        {
            get => _headers;
            set
            {
                _headers = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Headers)));
            }
        }

        private Header? _header = new Header();
        public Header? Header
        {
            get => _header;
            set
            {
                _header = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Header)));
            }
        }


        private Models.Position? _position_selectedItem;
        public Models.Position? Position_SelectedItem
        {
            get => _position_selectedItem;
            set
            {
                _position_selectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position_SelectedItem)));
            }
        }

        private ObservableCollection<Models.Position> _positions = new ObservableCollection<Models.Position>();
        public ObservableCollection<Models.Position> Positions
        {
            get => _positions;
            set
            {
                _positions = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Positions)));
            }
        }

        public MainPageViewModel()
        {
            _databaseService = new DatabaseService();
            _= LoadHeaders();
        }

        public async Task LoadHeaders()
        {
            var headers = await _databaseService.GetHeadersAsync();
            if (headers == null)
                return;

            Headers.Clear();
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

            var selectedItem = await _databaseService.GetHeaderAsync(new_id);
            Header_SelectedItem = selectedItem;

            return new_id;
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
            }
        }

        private bool _isExpander_listsExpended=true;
        public bool IsExpander_listsExpended
        {
            get => _isExpander_listsExpended;
            set
            {
                _isExpander_listsExpended = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpander_listsExpended)));

                Expander_listsIcon=value ? "collapse_icon.png" : "expand_icon.png";
            }
        }

        private string _expander_listsIcon;
        public string Expander_listsIcon
        {
            get => _expander_listsIcon;
            set
            {
                _expander_listsIcon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Expander_listsIcon)));
            }
        }

        private bool _expander_positionsExpended=true;
        public bool Expander_positionsExpended
        {
            get => _expander_positionsExpended;
            set
            {
                _expander_positionsExpended = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Expander_positionsExpended)));

                Expander_positionsIcon=value ? "collapse_icon.png" : "expand_icon.png";
            }
        }

        private string _expander_positionsIcon;
        public string Expander_positionsIcon
        {
            get => _expander_positionsIcon;
            set
            {
                _expander_positionsIcon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Expander_positionsIcon)));
            }
        }

        public async Task LoadPositions()
        {
            IsBusy = true;

            if (Header_SelectedItem == null)
                return;

            Positions.Clear();

            var _pos_arr = await _databaseService.GetPositionsAsync(Header_SelectedItem.Id);
            if (_pos_arr == null || _pos_arr.Count == 0)
            {
                IsBusy = false;
                return;
            }

            var sort_pos_arr = _pos_arr.OrderByDescending(x => x.IsCompleted).ToList();


            Positions = new ObservableCollection<Position>(sort_pos_arr);

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);

            IsBusy=false;
        }

        public async Task AddPosition(string PositionEntryText)
        {
            if(Header_SelectedItem  == null || string.IsNullOrEmpty(PositionEntryText)) return;

            var newPosition = new Models.Position { HeaderId = Header_SelectedItem.Id, Title = PositionEntryText, IsCompleted = false };
            await _databaseService.AddPositionAsync(newPosition);
            await LoadPositions();

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);
        }

        public async Task DeleteHeader(Models.Header header)
        {
            if(header == null) return;

            Header_SelectedItem = header;
            await DeletePositionsByHeaderIdAsync();
            await _databaseService.DeleteHeaderAsync(header.Id);
            await LoadHeaders();
            await LoadPositions();
        }

        public async Task UpdatePosition(Models.Position pos)
        {
            IsBusy=true;

            Position_SelectedItem = pos;
            await _databaseService.UpdatePositionAsync(pos);

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);

            IsBusy =false;
        }

        private void SetSortedPositionsList(List<Position> sortedItems)
        {
            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            Positions.Clear();
            foreach (var item in sortedItems)
            {
                Positions.Add(item);
            }
        }

        public async Task DeletePosition(Models.Position pos)
        {
            Position_SelectedItem= pos;
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
        }

        internal async Task EditTitleOfPosition(Position position, string title)
        {
            Position_SelectedItem = position;
            await _databaseService.EditPositionsTitleAsync(position, title);
            await LoadPositions();
        }

        public async Task ResetPositionsAsync()
        {
        if (Header_SelectedItem == null) return;

            await _databaseService.UpdateIsCompletedPositionsAsync(Header_SelectedItem.Id, false);
            await LoadPositions();
        }
    }
}
