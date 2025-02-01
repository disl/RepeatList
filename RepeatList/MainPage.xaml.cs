using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using RepeatList.Models;
using RepeatList.Services;

namespace TodoApp
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _databaseService;
        private List<Header> _headers;
        private List<Position> _positions;
        private Header _selectedHeader;
        private Position _selectedPosition;

        public MainPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadHeaders();
        }

        private async void LoadHeaders()
        {
            _headers = await _databaseService.GetHeadersAsync();
            HeaderListView.ItemsSource = _headers;
        }

        private async void OnAddHeaderClicked(object sender, EventArgs e)
        {
            var newHeader = new Header { ListName = HeaderEntry.Text, Date = DateTime.Now };
            await _databaseService.AddHeaderAsync(newHeader);
            HeaderEntry.Text = string.Empty;
            LoadHeaders();
        }

        private async void OnHeaderSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _selectedHeader = e.SelectedItem as Header;
            if (_selectedHeader != null)
            {
                _positions = await _databaseService.GetPositionsAsync(_selectedHeader.Id);
                PositionListView.ItemsSource = _positions;
            }
        }

        private async void OnAddPositionClicked(object sender, EventArgs e)
        {
            if (_selectedHeader != null)
            {
                var newPosition = new Position { HeaderId = _selectedHeader.Id, Title = PositionEntry.Text, IsCompleted = false };
                await _databaseService.AddPositionAsync(newPosition);
                PositionEntry.Text = string.Empty;
                _positions = await _databaseService.GetPositionsAsync(_selectedHeader.Id);
                PositionListView.ItemsSource = _positions;
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
                _positions = await _databaseService.GetPositionsAsync(_selectedHeader.Id);
                PositionListView.ItemsSource = _positions;
            }
        }
    }
}