using RepeatList.Models;
using RepeatList.Services;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace RepeatList.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private DatabaseService _databaseService;
        public event PropertyChangedEventHandler PropertyChanged;


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

        private ObservableCollection<Header>? _headers=new ObservableCollection<Header>();
        public ObservableCollection<Header>? Headers
        {
            get => _headers;
            set
            {
                _headers = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Headers)));
            }
        }

        private Header? _header=new Header();
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

        private ObservableCollection<Models.Position> _positions=new ObservableCollection<Models.Position>();
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

           
            Positions = new ObservableCollection<Position>( sort_pos_arr);

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);

            IsBusy=false;
        }

        public async Task AddPosition(string PositionEntryText)
        {
            var newPosition = new Models.Position { HeaderId = Header_SelectedItem.Id, Title = PositionEntryText, IsCompleted = false };
            await _databaseService.AddPositionAsync(newPosition);
            await LoadPositions();

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);
        }

        public async Task DeleteHeader(Models.Header header)
        {
            Header_SelectedItem = header;
            await DeletePositionsByHeaderIdAsync();
            await _databaseService.DeleteHeaderAsync(header.Id);
            await LoadHeaders();
            await LoadPositions();

            //var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            //SetSortedPositionsList(sorted_list);
        }

        public async Task UpdatePosition(Models.Position pos)
        {
            IsBusy=true;

            Position_SelectedItem = pos;
            await _databaseService.UpdatePositionAsync(pos);

            var sorted_list = Positions.OrderBy(x => x.IsCompleted).ToList();
            SetSortedPositionsList(sorted_list);
            //Positions = new ObservableCollection<Position>(); // new ObservableCollection<Position>(sorted_list);
            //Positions =  new ObservableCollection<Position>(sorted_list);

            //await LoadPositions();

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
            await _databaseService.DeletePositionsByHeaderIdAsync(Header_SelectedItem.Id);
            await LoadPositions();
        }
    }
}
