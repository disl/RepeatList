using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.Services;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using Position = RepeatList.Models.Position;


namespace RepeatList
{
    public partial class PositionsPage : ContentPage
    {
        private SetupPageViewModel SetupPageViewModel { get; set; }
        //private HelpPageViewModel HelpPageViewModel { get; set; }
        private PositionsPageViewModel ViewModel { get; set; }
        private CategoryPosition_PopUpViewModel m_CategoryPosition_PopUpViewModel = new CategoryPosition_PopUpViewModel();

        //var client = new DeepSeekClient("sk-a3240964efda4aa1aa6cf6ffcf9713b2");

        private IDispatcherTimer _timer;

        private double _lastScrollPosition_undone = 0;
        private object _lastVisibleItem_undone = null;
        private double _lastScrollPosition_done = 0;
        private object _lastVisibleItem_done = null;

        //private readonly ISpeechToText _speechToText;

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

            try
            {
                ViewModel.IsBusy = true;

                ViewModel.Duplicate_entries_add = Preferences.Get("duplicate_entries_add", true);
                ViewModel.Menu_icon = ViewModel.Duplicate_entries_add ? "menu.png" : "menu_alert.png";

                //if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
                //    HeaderListView.SelectedItem=ViewModel.Headers[0];

                await ViewModel.LoadPositions();

                if (SetupPageViewModel.SelectedItem != null)
                {
                    SetCurrentCulture(SetupPageViewModel.SelectedItem.DefaultLanguage);
                }

                ViewModel.ItemSource_KindOfSorting = new ObservableCollection<CMBType_String>
                {
                 new CMBType_String(Properties.Resources.sort_by_time, "date"),
                 new CMBType_String(Properties.Resources.sort_by_alphabet, "alpha" ),
                 new CMBType_String(Properties.Resources.sort_by_category, "category" )
                };

                // Undone-Picker
                if (SortingPicker_undone == null)
                    SortingPicker_undone = new Picker();
                SortingPicker_undone.ItemsSource = ViewModel.ItemSource_KindOfSorting_undone.ToObservableCollection();
                var _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name_undone, "alpha");
                if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                    ViewModel.SelectedItem_KindOfSorting_undone = ViewModel.ItemSource_KindOfSorting_undone.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
                else
                    ViewModel.SelectedItem_KindOfSorting_undone = ViewModel.ItemSource_KindOfSorting_undone.FirstOrDefault(x => x.Value == "alpha");
                SortingPicker.SelectedIndex = ViewModel.ItemSource_KindOfSorting.IndexOf(ViewModel.SelectedItem_KindOfSorting);

                // Done-Picker
                if (SortingPicker == null)
                    SortingPicker = new Picker();
                SortingPicker.ItemsSource = ViewModel.ItemSource_KindOfSorting.ToObservableCollection();
                _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name, "date");
                if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                    ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
                else
                    ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == "date");
                SortingPicker.SelectedIndex = ViewModel.ItemSource_KindOfSorting.IndexOf(ViewModel.SelectedItem_KindOfSorting);

                string tmp_lists = Properties.Resources.Lists.ToUpper();

                ViewModel.Label_lists = tmp_lists;

                ViewModel.InitLabels();

                if (ViewModel.Header_SelectedItem.IsSynchronized)
                {
                    _timer = Dispatcher.CreateTimer();
                    _timer.Interval = TimeSpan.FromSeconds(15);
                    _timer.Tick += _timer_Tick;
                    _timer.Start();
                }

                //SetHeader();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
            finally { ViewModel.IsBusy = false; }
        }

        //private void SetHeader()
        //{
        //    ViewModel.IsRezeptVisible = false;

        //    if (ViewModel.Positions_undone_filterd != null && !string.IsNullOrEmpty(ViewModel.Positions_undone_filterd[0].Title)
        //        && ViewModel.Positions_undone_filterd[0].Title.Substring(0, 1) == "_")
        //    {
        //        var ind_ = ViewModel.Positions_undone_filterd[0].Title.IndexOf("_");
        //        var ind_dp = ViewModel.Positions_undone_filterd[0].Title.IndexOf(":");
        //        if (ind_dp > ind_)
        //            RezeptLabel.Text = ViewModel.Positions_undone_filterd[0].Title.Substring(
        //                ind_dp, ViewModel.Positions_undone_filterd[0].Title.Length - (ind_dp - ind_));
        //        else
        //            RezeptLabel.Text = ViewModel.Positions_undone_filterd[0].Title;

        //        ViewModel.IsRezeptVisible = true;

        //        //var headerStack = new StackLayout { Orientation = StackOrientation.Vertical };
        //        //headerStack.Children.Add(new Label
        //        //{
        //        //    Text = ViewModel.Positions_undone_filterd[0].Title,
        //        //    TextColor= Application.Current.RequestedTheme == AppTheme.Light
        //        //                ? (Color)Application.Current.Resources["PrimaryTextColor"]
        //        //                : (Color)Application.Current.Resources["PrimaryTextColorLight"]
        //        //});

        //        //PositionListView.Header = headerStack;

        //        ViewModel.Positions_undone_filterd.RemoveAt(0);
        //    }
        //}

        private void OnCollectionView_Undone_Scrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            // Vertikale Position speichern
            _lastScrollPosition_undone = (int)e.VerticalOffset;

            //// Optional: Erstes sichtbares Item speichern
            //if (e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex < Items.Count)
            //{
            //    _lastVisibleItem = Items[e.FirstVisibleItemIndex];
            //}
        }

        private void OnCollectionView_Done_Scrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            // Vertikale Position speichern
            _lastScrollPosition_done = (int)e.VerticalOffset;

            //// Optional: Erstes sichtbares Item speichern
            //if (e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex < Items.Count)
            //{
            //    _lastVisibleItem = Items[e.FirstVisibleItemIndex];
            //}
        }

        private async void _timer_Tick(object? sender, EventArgs e)
        {
            if (ViewModel.IsBusy) return;

            ViewModel.IsBusy = true;

            if (!ViewModel.Header_SelectedItem.IsSynchronized)
                return;

            await ViewModel.Sync_list_downClicked(ViewModel.Header_SelectedItem);

            await ViewModel.LoadPositions();

            //SetHeader();

            ViewModel.IsBusy = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_timer != null)
                _timer.Stop();
        }

        private void SetCurrentCulture(string curr_culture)
        {
            //CultureInfo ci = new CultureInfo("en");
            var ci = new CultureInfo(curr_culture);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }


        #region POSITIONS

        //private async void OnAddPositionClicked(object sender, EventArgs e)
        //{
        //    if (ViewModel.Header_SelectedItem == null)
        //        return;

        //    ViewModel.IsBusy = true;

        //    var promptPage = new InputTextWithMicrophone();
        //    await Navigation.PushModalAsync(promptPage);

        //    if (promptPage.Result == null)
        //    {
        //        ViewModel.IsBusy = false;
        //        return;
        //    }

        //    string _input = await promptPage.Result;

        //    await InputPositions(_input);
        //}

        private async Task InputPositions(string _input)
        {
            Header? json = null;

            try
            {
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
                            header.UpdatedAt = DateTime.Now.ToUniversalTime();
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
                            json.UpdatedAt = DateTime.Now.ToUniversalTime();
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
                                new_pos.UpdatedAt = DateTime.Now.ToUniversalTime();
                                new_pos.Title = item.Trim();
                                await ViewModel.AddPosition(new_pos, true);
                            }
                        }
                        else
                        {
                            var new_pos = new Position();
                            new_pos.HeaderId = ViewModel.Header_SelectedItem.Id;
                            new_pos.Title = _input;
                            new_pos.UpdatedAt = DateTime.Now.ToUniversalTime();
                            await ViewModel.AddPosition(new_pos, true);
                        }
                    }
                    else
                    {
                        var new_pos = new Position();
                        new_pos.HeaderId = ViewModel.Header_SelectedItem.Id;
                        new_pos.Title = _input;
                        new_pos.UpdatedAt = DateTime.Now.ToUniversalTime();
                        await ViewModel.AddPosition(new_pos, true);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }

        }

        private async void OnDeletePositionClicked(object sender, EventArgs e)
        {
            if (ViewModel.IsBusy) return;

            try
            {
                ViewModel.IsBusy = true;

                var button = sender as ImageButton;
                if (button?.CommandParameter is Position pos)
                {
                    await ViewModel.DeletePosition(pos);
                }

                ViewModel.IsBusy = false;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
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

            try
            {
                if (ViewModel.HeaderSelected)
                {
                    ViewModel.HeaderSelected = false;
                    return;
                }

                if (sender is CheckBox switchControl &&
                    e != null &&
                    switchControl.BindingContext != null &&
                    switchControl.BindingContext is Position position)
                {
                    ViewModel.IsBusy = true;

                    position.IsCompleted = e.Value;
                    await ViewModel.UpdatePosition(position);

                    ViewModel.IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void OnCheckedButtonClicked(object sender, EventArgs e)
        {
            if (ViewModel.IsBusy) return;

            if (e == null) return;

            Position? position = null;

            _currentlyOpenSwipeView = null;

            if (sender is Microsoft.Maui.Controls.ImageButton switchControl &&
                   switchControl.BindingContext != null &&
                   switchControl.BindingContext is Position _position_image)
            {
                position = _position_image;
            }

            if (position == null)
            {
                if (sender is Microsoft.Maui.Controls.SwipeItem swipeItem &&
                   swipeItem.BindingContext != null &&
                   swipeItem.BindingContext is Position _position_swipeItem)
                {
                    _currentlyOpenSwipeView = swipeItem.Parent.Parent as SwipeView;
                    position = _position_swipeItem;
                }
            }

            if (position == null)
            {
                if (sender is Microsoft.Maui.Controls.SwipeView swipeItem &&
                   swipeItem.LeftItems != null &&
                   swipeItem.LeftItems.BindingContext is Position _position_swipeItem)
                {
                    _currentlyOpenSwipeView = swipeItem;
                    position = _position_swipeItem;
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(MySearchBar.Text))
                    MySearchBar.Text = "";

                if (position != null)
                {
                    ViewModel.IsBusy = true;

                    position.IsCompleted = !position.IsCompleted;  // e.Value;
                                                                   // position.UpdatedAt = DateTime.Now.ToUniversalTime();
                    await ViewModel.UpdatePosition(position);

                    //SetHeader();

                    // SwipeView SOFORT wieder öffnen
                    if (_currentlyOpenSwipeView != null)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Task.Delay(50); // Kurze Verzögerung
                            _currentlyOpenSwipeView.Open(OpenSwipeItem.LeftItems);
                        });
                    }

                    ViewModel.IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }

        private async void OnPositionSelected_new(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection == null || e.CurrentSelection.Count != 1)
                return;

            ViewModel.Position_selectedItem = e.CurrentSelection[0] as Position;

            var popup = new Positions_Edit(ViewModel.Position_selectedItem);
            var result = await Shell.Current.ShowPopupAsync<object>(popup);

            if (result == null || result.Result == null)
                return;

            if (result.Result is Position)
            {
                var position = (Position)result.Result;
                string new_title = position.Title.Trim();
                if (string.IsNullOrWhiteSpace(new_title))
                    return;
                // Update the title of the position
                if (ViewModel.Position_selectedItem != null && ViewModel.Position_selectedItem.Id == position.Id)
                {
                    ViewModel.Position_selectedItem.Title = new_title;

                    if (await ViewModel.EditTitleOfPosition(position, new_title))

                        // Refresh the selected item to update the UI
                        //PositionListView.ScrollTo(ViewModel.Position_selectedItem, null, ScrollToPosition.MakeVisible, false);
                        MySearchBar.Text = new_title;
                }
            }
            else if (result.Result is string && result.Result.ToString().ToLower() == "delete")
            {
                // Delete the position
                if (ViewModel.Position_selectedItem != null)
                {
                    if (await ViewModel.DeletePosition(ViewModel.Position_selectedItem))
                    {
                        ViewModel.Position_selectedItem = null;
                        //PositionListView.ScrollTo(e.CurrentSelection[0], position, ScrollToPosition.Center, false);
                    }
                }
            }
        }

        private async void SortingPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender == null || ViewModel == null || ViewModel.SelectedItem_KindOfSorting == null)
                    return;
                var picker = sender as Picker;
                await ViewModel.LoadPositions();

                //SetHeader();

                Preferences.Set(ViewModel.SelectedItem_KindOfSorting_key_name, ViewModel.SelectedItem_KindOfSorting.Value);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void SortingPicker_undone_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            await ViewModel.LoadPositions();

            //SetHeader();

            Preferences.Set(ViewModel.SelectedItem_KindOfSorting_key_name_undone, ViewModel.SelectedItem_KindOfSorting_undone.Value);
        }

        private async void OnIsSynchronizedClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void Sync_list_upClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void Sync_list_downClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void Positions_inputEntry_Completed(object sender, EventArgs e)
        {
            await ForPositions_input();
        }

        private async Task ForPositions_input()
        {
            ViewModel.IsBusy = true;

            await InputPositions(ViewModel.InputText);

            ViewModel.InputText = string.Empty;

            await Application.Current.MainPage.DisplaySnackbar(Properties.Resources.Operation_successfully_completed,
                visualOptions: new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb(Constantes.Color_Success_string),
                    TextColor = Colors.White,
                },
                duration: TimeSpan.FromSeconds(2)
                );

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
            try
            {
                string searchText = e.NewTextValue?.ToLower() ?? "";

                //ViewModel.FilteredList.Clear();
                ViewModel.Positions_undone_filterd.Clear();
                ViewModel.Positions_done_filtered.Clear();

                // Undone
                if (string.IsNullOrEmpty(searchText))
                {
                    //ViewModel.Positions_undone_filterd = new ObservableCollection<Position>(ViewModel.Positions_undone);
                    foreach (var item in ViewModel.Positions_undone)
                        ViewModel.Positions_undone_filterd.Add(item);
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
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private async void OnHelpButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HelpPage(HelpPageViewModel.HelpTopicThemasEnum.InputTextBox, new CultureInfo(ViewModel.CurrentCulture)));
        }

        private async void OpenMenu(object sender, EventArgs e)
        {
            var popup = new Positions_PopUpMenu(ViewModel.SupabaseService_ready);
            var options = new PopupOptions
            {
                CanBeDismissedByTappingOutsideOfPopup = true,

            };
            var result = await PopupExtensions.ShowPopupAsync<string>(this, popup, options);
            //var result = await Shell.Current.ShowPopupAsync<string>(popup, options);
            //var result = await Application.Current.MainPage.Navigation.ShowPopupAsync<string>(popup, options);

            if (result == null || result.Result == null)
                return;

            switch (result.Result)
            {
                case "Export_not_completed_as_a_text_list":
                    if (ViewModel.Positions_undone.Count == 0)
                    {
                        await DisplayAlert(Properties.Resources.Export_list,
                            Properties.Resources.List_is_empty, "OK");
                        return;
                    }
                    await ViewModel.Export_list_textClicked();
                    break;
            }

            //await popup.CloseAsync();

            ViewModel.Duplicate_entries_add = Preferences.Get("duplicate_entries_add", true);
        }

        private async void Category_colorClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.CommandParameter is Position position)
            {
                if (position == null || position.Title == null || position.Category == null)
                    return;

                // Hier category color change
                var categories_list = ViewModel.Positions.Select(x => x.Category).Distinct().OrderBy(x => x).ToObservableCollection();

                var popup = new CategoryPosition_PopUp(ViewModel.m_Categories_listType_list, position.Category);
                var result = await Shell.Current.ShowPopupAsync<string>(popup);
                if (result != null)
                {
                    await m_CategoryPosition_PopUpViewModel.UpdateOrAdd(position.Title, result.Result);
                    await ViewModel.RefreshColors();
                }
            }
        }


        private Positions_Edit? _currentPopup_Positions_Edit;
        private SwipeView? _currentlyOpenSwipeView;
        private SwipeDirection _currentSwipeDirection;
        private bool _swipeCompleted;

        private async void PositionListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            try
            {
                if (e.Item == null)
                    return;

                ViewModel.Position_selectedItem = e.Item as Position;

                if (_currentPopup_Positions_Edit == null)
                {

                    _currentPopup_Positions_Edit = new Positions_Edit(ViewModel.Position_selectedItem);

                    //var popup = new Positions_Edit(ViewModel.Position_selectedItem);
                    var result = await this.ShowPopupAsync<object>(_currentPopup_Positions_Edit);

                    // Nach dem Schließen Instanz vergessen
                    _currentPopup_Positions_Edit = null;

                    if (result == null || result.Result == null)
                        return;

                    if (result.Result is Position)
                    {
                        var position = (Position)result.Result;
                        string new_title = position.Title.Trim();
                        if (string.IsNullOrWhiteSpace(new_title))
                            return;
                        // Update the title of the position
                        if (ViewModel.Position_selectedItem != null && ViewModel.Position_selectedItem.Id == position.Id)
                        {
                            ViewModel.Position_selectedItem.Title = new_title;

                            if (await ViewModel.EditTitleOfPosition(position, new_title))

                                // Refresh the selected item to update the UI
                                //PositionListView.ScrollTo(ViewModel.Position_selectedItem, null, ScrollToPosition.MakeVisible, false);
                                //MySearchBar.Text = new_title;
                                PositionListView.ScrollTo(ViewModel.Position_selectedItem, ScrollToPosition.Center, true);
                        }
                    }
                    else if (result.Result.ToString().ToLower() == "delete")
                    {
                        // Delete the position
                        if (ViewModel.Position_selectedItem != null)
                        {
                            if (await ViewModel.DeletePosition(ViewModel.Position_selectedItem))
                            {
                                ViewModel.Position_selectedItem = null;
                                //PositionListView.ScrollTo(e.CurrentSelection[0], position, ScrollToPosition.Center, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                throw;
            }
        }

        private void ExpanderChanged(object sender, ExpandedChangedEventArgs e)
        {

            ViewModel.IsRezeptExpanded = e.IsExpanded;

        }

        private async void OnAudioRecognationButton_Clicked(object sender, EventArgs e)
        {
            var popup = new VoiceRecognitionPage();
            var result = await Shell.Current.ShowPopupAsync<string>(popup);
            if (result != null && !string.IsNullOrEmpty(result.Result))
            {
                ViewModel.IsBusy = true;

                var client = new DeepSeekClient();

                var list_text = await client.TranscribeToShoppingList(result.Result.Replace("\n", ""));
                if (!string.IsNullOrEmpty(list_text))
                {

                    await InputPositions(list_text);
                }

                ViewModel.IsBusy = false;
            }
        }

        private void OnSwipeStarted_Top(object sender, SwipeStartedEventArgs e)
        {
            _currentSwipeDirection = e.SwipeDirection;
            _swipeCompleted = false;
        }

        private void OnSwipeChanging_Top(object sender, SwipeChangingEventArgs e)
        {
            // Könnte für visuelles Feedback verwendet werden
            // z.B. Hintergrundfarbe ändern beim Swipe
        }

        private void OnSwipeEnded_Top(object sender, SwipeEndedEventArgs e)
        {
            if (_swipeCompleted) return;

            var swipeView = sender as SwipeView;
            if (swipeView == null || swipeView.BindingContext is not Position item)
                return;

            // Automatische Aktion basierend auf Swipe-Richtung
            switch (_currentSwipeDirection)
            {
                case SwipeDirection.Left:
                case SwipeDirection.Right:
                    // Rechts-Swipe: Automatisch "Done"
                    OnCheckedButtonClicked(swipeView, new EventArgs());
                    _swipeCompleted = true;
                    break;

               
            }
        }
    }
}
