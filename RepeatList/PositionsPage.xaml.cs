using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RepeatList
{
    public partial class PositionsPage : ContentPage
    {
        private SetupPageViewModel SetupPageViewModel { get; set; }
        private HelpPageViewModel HelpPageViewModel { get; set; }
        private PositionsPageViewModel ViewModel { get; set; }

        public PositionsPage(Header selectedItem)
        {
            InitializeComponent();

            ViewModel = new PositionsPageViewModel(selectedItem);
            BindingContext = ViewModel;
            SetupPageViewModel = new SetupPageViewModel();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.IsBusy = true;

            //if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
            //    HeaderListView.SelectedItem=ViewModel.Headers[0];

            await ViewModel.LoadPositions();

            if (SetupPageViewModel.SelectedItem != null)
            {
                SetCurrentCulture(SetupPageViewModel.SelectedItem.DefaultLanguage);
            }

            ViewModel.ItemSource_KindOfSorting = new ObservableCollection<CMBType_String>
            {
             new CMBType_String(Properties.Resources.sort_by_date, "date"),
             new CMBType_String(Properties.Resources.sort_by_alphabet, "alpha" )
            };

            if (SortingPicker == null)
                SortingPicker = new Picker();

            SortingPicker.ItemsSource = ViewModel.ItemSource_KindOfSorting.ToObservableCollection();
            var _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name, "date");
            if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
            else
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == "date");
            SortingPicker.SelectedIndex = ViewModel.ItemSource_KindOfSorting.IndexOf(ViewModel.SelectedItem_KindOfSorting);

            string tmp_lists = Properties.Resources.Lists.ToUpper();

            ViewModel.Label_lists = tmp_lists;

            ViewModel.InitLabels();

            ViewModel.IsBusy = false;
        }

        private void SetCurrentCulture(string curr_culture)
        {
            //CultureInfo ci = new CultureInfo("en");
            var ci = new CultureInfo(curr_culture);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }




        #region POSITIONS

        private async void OnAddPositionClicked(object sender, EventArgs e)
        {
            if (ViewModel.Header_SelectedItem == null)
                return;

            ViewModel.IsBusy = true;

            var promptPage = new InputTextWithMicrophone();
            await Navigation.PushModalAsync(promptPage);

            if (promptPage.Result == null)
            {
                ViewModel.IsBusy = false;
                return;
            }

            string _input = await promptPage.Result;

            await InputPositions(_input);
        }

        private async Task InputPositions(string _input)
        {
            Header? json = null;

            ViewModel.IsBusy = true;

            if (string.IsNullOrEmpty(_input) || ViewModel.Headers == null)
            {
                ViewModel.IsBusy = false;
                return;
            }

            try
            {
                json = JsonConvert.DeserializeObject<Header>(_input);
            }
            catch { json = null; }

            if (json != null)
            {
                if (ViewModel.Headers != null)
                {
                    var header = ViewModel.Headers.FirstOrDefault(x => x.Id == json.Id);
                    if (header != null)
                    {
                        ViewModel.Header_SelectedItem = header;

                        // Existing Header
                        header.UpdatedAt = DateTime.Now;
                        // Add to existing positions
                        foreach (var pos in json.Positions)
                        {
                            pos.Title = pos.Title.Trim() + " (+)";
                            await ViewModel.AddPosition(pos, false);
                        }
                    }
                    else
                    {
                        // Add new header
                        json.UpdatedAt = DateTime.Now;
                        var new_header = await ViewModel.AddHeader(json.ListName, json.Id);

                        ViewModel.Header_SelectedItem = new_header;

                        // Add new positions
                        foreach (var pos in json.Positions)
                        {
                            pos.HeaderId = new_header.Id;
                            pos.Title = pos.Title.Trim() + " (+)";
                            await ViewModel.AddPosition(pos, false);
                        }
                    }
                }
            }
            else
            {
                if (ViewModel.Header_SelectedItem == null)
                {
                    ViewModel.IsBusy = false;
                    return;
                }

                if (_input.Contains(";"))  // && !ViewModel.Replace_old_word_when_inserting)
                {
                    var items_list = _input.Split(";").ToList();
                    if (items_list.Count > 0)
                    {
                        foreach (var item in items_list)
                        {
                            if (string.IsNullOrEmpty(item))
                                continue;

                            var new_pos = new Position();
                            new_pos.HeaderId = ViewModel.Header_SelectedItem.Id;
                            new_pos.UpdatedAt = DateTime.Now;
                            new_pos.Title = item.Trim();
                            await ViewModel.AddPosition(new_pos, true);
                        }
                    }
                    else
                    {
                        var new_pos = new Position();
                        new_pos.HeaderId = ViewModel.Header_SelectedItem.Id;
                        new_pos.Title = _input;
                        new_pos.UpdatedAt = DateTime.Now;
                        await ViewModel.AddPosition(new_pos, true);
                    }
                }
                else
                {
                    var new_pos = new Position();
                    new_pos.HeaderId = ViewModel.Header_SelectedItem.Id;
                    new_pos.Title = _input;
                    new_pos.UpdatedAt = DateTime.Now;
                    await ViewModel.AddPosition(new_pos, true);
                }
            }

            ViewModel.IsBusy = false;
        }

        private async void OnDeletePositionClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Position pos)
            {
                await ViewModel.DeletePosition(pos);
            }
        }

        private async void OnEditPositionClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Position position)
            {
                string new_title = await DisplayPromptAsync(Properties.Resources.Input, Properties.Resources.Enter_new_position_title, "OK", Properties.Resources.Cancel, initialValue: position.Title);
                if (!string.IsNullOrWhiteSpace(new_title))
                {
                    await ViewModel.EditTitleOfPosition(position, new_title);
                }
            }
        }

        private async void OnResetPositionsClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert(Properties.Resources.ResetPositions, Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
            if (answer)
            {
                await ViewModel.ResetPositionsAsync();
            }
        }

        #endregion


        #region Coffee

        private async void CoffeeButtonClicked(object sender, EventArgs e)
        {
            string url = "https://Ko-fi.com/disl";
            await Launcher.OpenAsync(new Uri(url));
        }


        #endregion


        private async void OnPositionChecked(object sender, CheckedChangedEventArgs e)
        {
            if (ViewModel.IsBusy) return;
            if (ViewModel.HeaderSelected)
            {
                ViewModel.HeaderSelected = false;
                return;
            }

            //ViewModel.IsBusy=true;

            if (sender is Microsoft.Maui.Controls.CheckBox switchControl &&
                e != null &&
                switchControl.BindingContext != null &&
                switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = e.Value;
                position.UpdatedAt = DateTime.Now;
                await ViewModel.UpdatePosition(position);

                ViewModel.IsBusy = false;
            }
        }

        private async void OnCheckedButtonClicked(object sender, EventArgs e)
        {
            if (ViewModel.IsBusy) return;
            //if (ViewModel.HeaderSelected)
            //{
            //    ViewModel.HeaderSelected=false;
            //    return;
            //}

            //ViewModel.IsBusy=true;

            searchBar.Text = "";

            if (sender is Microsoft.Maui.Controls.ImageButton switchControl &&
                e != null &&
                switchControl.BindingContext != null &&
                switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = !position.IsCompleted;  // e.Value;
                position.UpdatedAt = DateTime.Now;
                await ViewModel.UpdatePosition(position);

                ViewModel.IsBusy = false;
            }
        }

        private void OnPositionSelected_new(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.Position_selectedItem = e.CurrentSelection as Position;
        }

        private async void SortingPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            await ViewModel.LoadPositions();
            Preferences.Set(ViewModel.SelectedItem_KindOfSorting_key_name, ViewModel.SelectedItem_KindOfSorting.Value);
        }

        private async void OnIsSynchronizedClicked(object sender, EventArgs e)
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

        private async void Sync_list_upClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;

                    await ViewModel.Sync_list_upClicked();
                }
            }
        }

        private async void Sync_list_downClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Would_you_like_to_start_synchronisation_now,
                Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    ViewModel.Header_SelectedItem = header;

                    await ViewModel.Sync_list_downClicked(header);

                }
            }
        }

        private async void Positions_inputEntry_Completed(object sender, EventArgs e)
        {
            await ForPositions_input();

            //Positions_inputEntry.IsEnabled = false;
            //Positions_inputEntry.IsEnabled = true;
        }

        private async Task ForPositions_input()
        {
            ViewModel.IsBusy = true;

            await InputPositions(ViewModel.InputText);

            ViewModel.InputText = string.Empty;

            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.Operation_successfully_completed);

            ViewModel.IsBusy = false;
        }

        private void OnSemicolonButton_Clicked(object sender, EventArgs e)
        {
            ViewModel.InputText += ";";
        }

        private void Positions_inputEntry_Focused(object sender, FocusEventArgs e)
        {
            ViewModel.IsExpander_listsExpended = false;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? "";

            //ViewModel.FilteredList.Clear();
            ViewModel.Positions_undone_filterd.Clear();
            ViewModel.Positions_done_filtered.Clear();

            // Undone
            if (string.IsNullOrEmpty(searchText))
            {
                ViewModel.Positions_undone_filterd = new ObservableCollection<Position>(ViewModel.Positions_undone);
            }
            else
            {
                foreach (var item in ViewModel.Positions_undone.Where(x => x.Title.ToLower().Contains(searchText.ToLower())))
                {
                    ViewModel.Positions_undone_filterd.Add(item);
                }
            }

            // Done
            if (string.IsNullOrEmpty(searchText))
            {
                ViewModel.Positions_done_filtered = new ObservableCollection<Position>(ViewModel.Positions_done);
            }
            else
            {
                foreach (var item in ViewModel.Positions_done.Where(x => x.Title.ToLower().Contains(searchText.ToLower())))
                {
                    ViewModel.Positions_done_filtered.Add(item);
                }
            }
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {

        }

        private async void OnHelpButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HelpPage( HelpPageViewModel.HelpTopicThemasEnum.InputTextBox, new CultureInfo(ViewModel.CurrentCulture)));
        }
    }

}
