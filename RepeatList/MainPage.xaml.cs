using RepeatList.Models;
using RepeatList.ViewModels;

namespace RepeatList
{
    public partial class MainPage : ContentPage
    {
        private App? m_application;

        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            InitializeComponent();

            ViewModel = BindingContext as  MainPageViewModel;

            if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
                HeaderListView.SelectedItem=ViewModel.Headers[0];

            Application.Current.UserAppTheme = AppTheme.Dark;
        }

        #region HEADER

        private async void OnAddHeaderClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(HeaderEntry.Text))
            {
                HeaderEntry.Focus();
            }
            else
            {
                var new_id = await ViewModel.AddHeader(HeaderEntry.Text);
                HeaderEntry.Text = string.Empty;
            }
        }

        private async void OnHeaderSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var _selectedHeader = e.SelectedItem as Header;
            if (_selectedHeader != null)
            {
                ViewModel.Header_SelectedItem= _selectedHeader;
                await ViewModel.LoadPositions();
            }
        }

        private async void OnDeleteHeaderClicked(object sender, EventArgs e)
        {
            if (ViewModel.Header_SelectedItem != null)
            {
                bool answer = await DisplayAlert("Delete list", "Are you sure?", "Yes", "No");
                if (answer)
                    await ViewModel.DeleteHeader(ViewModel.Header_SelectedItem);
            }
        }

        private async void OnCopyHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync("Input", "Enter list name:");
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    await ViewModel.CopyHeader(header, new_list_name);
                }
            }
        }

        private void OnClearHeaderEntryClicked(object sender, EventArgs e)
        {
            HeaderEntry.Text = string.Empty;
            HeaderEntry.Focus();
        }


        #endregion


        #region POSITIONS

        private async void OnAddPositionClicked(object sender, EventArgs e)
        {
            if (ViewModel.Header_SelectedItem != null)
            {
                if (string.IsNullOrEmpty(PositionEntry.Text))
                {
                    PositionEntry.Focus();
                }
                else
                {
                    await ViewModel.AddPosition(PositionEntry.Text);
                    PositionEntry.Text = string.Empty;
                }
            }
        }

        private async void OnPositionToggled(object sender, ToggledEventArgs e)
        {
            if (ViewModel.IsBusy) return;

            if (sender is Microsoft.Maui.Controls.Switch switchControl && e !=null  && switchControl.BindingContext != null && switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = e.Value;
                await ViewModel.UpdatePosition(position);

                ViewModel.IsBusy = false;
            }
        }

        private void OnPositionSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ViewModel.Position_SelectedItem = e.SelectedItem as Position;
        }

        private async void OnDeletePositionClicked(object sender, EventArgs e)
        {
            if (ViewModel.Position_SelectedItem != null)
            {
                await ViewModel.DeletePosition(ViewModel.Position_SelectedItem);
            }
        }

        private void OnClearPositionEntryClicked(object sender, EventArgs e)
        {
            PositionEntry.Text = string.Empty;
            PositionEntry.Focus();
        }

        #endregion

        private async void OnEditHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync("Input", "Enter new list name:", initialValue: header.ListName);
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    await ViewModel.EditNameHeader(header, new_list_name);
                }
            }
        }

        private async void OnEditPositionClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Position position)
            {
                string new_title = await DisplayPromptAsync("Input", "Enter new position title:", initialValue: position.Title);
                if (!string.IsNullOrWhiteSpace(new_title))
                {
                    await ViewModel.EditTitleOfPosition(position, new_title);
                }
            }
        }

        private async void CoffeeButtonClicked(object sender, EventArgs e)
        {
            string url = "https://Ko-fi.com/dnepr65";
            await Launcher.OpenAsync(new Uri(url));
        }       
    }

}
