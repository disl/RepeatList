using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;

namespace RepeatList
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _databaseService;
        private List<Header> _headers;
        //private List<Position> _positions;
        private ObservableCollection<Position> Positions { get; set; } = new();
        private Header _selectedHeader;
        private Position _selectedPosition;
        //private bool _startUpdate;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = this; // Damit das Binding auf `Positions` funktioniert

            _databaseService = new DatabaseService();
            _ = LoadHeaders();
            if (_headers != null && _headers.Count > 0)
                HeaderListView.SelectedItem=_headers[0];
        }


        private async Task LoadHeaders()
        {
            _headers = await _databaseService.GetHeadersAsync();
            HeaderListView.ItemsSource = _headers;
        }

        private async void OnAddHeaderClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(HeaderEntry.Text))
            {
                //PositionEntry.Text="???";
                //HeaderEntry.SelectionLength=3;
                //HeaderEntry.CursorPosition=0;
                HeaderEntry.Focus();
            }
            else
            {
                var newHeader = new Header { ListName = HeaderEntry.Text, Date = DateTime.Now };
                var new_id =  await _databaseService.AddHeaderAsync(newHeader);

                HeaderEntry.Text = string.Empty;
                await LoadHeaders();
            }
        }

        private async void OnHeaderSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _selectedHeader = e.SelectedItem as Header;
            if (_selectedHeader != null)
            {
                await LoadPositions(_selectedHeader.Id);
            }
        }

        private async void OnAddPositionClicked(object sender, EventArgs e)
        {
            if (_selectedHeader != null)
            {
                if (string.IsNullOrEmpty(PositionEntry.Text))
                {
                    //PositionEntry.Text="???";
                    //PositionEntry.SelectionLength=3;
                    //PositionEntry.CursorPosition=0;
                    PositionEntry.Focus();
                }
                else
                {
                    var newPosition = new Position { HeaderId = _selectedHeader.Id, Title = PositionEntry.Text, IsCompleted = false };
                    await _databaseService.AddPositionAsync(newPosition);
                    PositionEntry.Text = string.Empty;
                    //_positions = await _databaseService.GetPositionsAsync(_selectedHeader.Id);
                    //PositionListView.ItemsSource = _positions;
                    await LoadPositions(_selectedHeader.Id);
                    //PositionListView.ItemsSource = Positions;
                }
            }
        }


        private async Task LoadPositions(int headerId)
        {
            var list = await _databaseService.GetPositionsAsync(headerId);

            Positions.Clear();

            if (list==null)
                return;

            foreach (var pos in list)
            {
                Positions.Add(pos); 
            }
        }

        private async void OnPositionToggled(object sender, ToggledEventArgs e)
        {
            if (IsBusy == false && sender is Switch switchControl && switchControl.BindingContext is Position position)
            {
                position.IsCompleted = e.Value;
                await _databaseService.UpdatePositionAsync(position); // Änderung in SQLite speichern

                //var updatedPositions = await _databaseService.GetPositionsAsync(position.HeaderId);
                //PositionListView.ItemsSource = null; // Sicherstellen, dass die UI aktualisiert wird
                //PositionListView.ItemsSource = updatedPositions;

                IsBusy=true;
                await LoadPositions(_selectedHeader.Id);
                IsBusy = false;
            }
        }

        private void OnPositionSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _selectedPosition = e.SelectedItem as Position;
        }

        private async void OnDeletePositionClicked(object sender, EventArgs e)
        {
            if (_selectedPosition != null)
            {
                await _databaseService.DeletePositionAsync(_selectedPosition.Id);
                //_positions = await _databaseService.GetPositionsAsync(_selectedHeader.Id);
                //PositionListView.ItemsSource = _positions;
                await LoadPositions(_selectedHeader.Id);
                //PositionListView.ItemsSource = Positions;
            }
        }

        private async void OnDeleteHeaderClicked(object sender, EventArgs e)
        {
            if (_selectedHeader != null)
            {
                await _databaseService.DeletePositionsByHeaderIdAsync(_selectedHeader.Id);
                await _databaseService.DeleteHeaderAsync(_selectedHeader.Id);
                await LoadHeaders();
                await LoadPositions(_selectedHeader.Id);
            }
        }

        private void OnCopyHeaderClicked(object sender, EventArgs e)
        {
            if (_selectedHeader != null)
            {
            //    var newHeader = new Header { ListName = HeaderEntry.Text, Date = DateTime.Now };
            //    await _databaseService.AddHeaderAsync(newHeader);
            //    HeaderEntry.Text = string.Empty;
            //    await LoadHeaders();
            }
        }
    }

}
