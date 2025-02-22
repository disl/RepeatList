using CommunityToolkit.Maui.Core.Extensions;
using Newtonsoft.Json;
using RepeatList.Models;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RepeatList
{
    public partial class MainPage : ContentPage
    {
        public SetupPageViewModel SetupPageViewModel { get; set; }
        public MainPageViewModel ViewModel { get; set; }

        public MainPage()  //ISpeechToText speechToText)
        {
            InitializeComponent();

            //ViewModel = BindingContext as  MainPageViewModel;
            ViewModel = new MainPageViewModel();
            BindingContext = ViewModel;

            ViewModel.IsBusy=true;

            if (ViewModel != null && ViewModel.Headers != null && ViewModel.Headers.Count > 0)
                HeaderListView.SelectedItem=ViewModel.Headers[0];

            SetupPageViewModel = new SetupPageViewModel();
            if (SetupPageViewModel.SelectedItem != null)
            {
                SetCurrentCulture(SetupPageViewModel.SelectedItem.DefaultLanguage);
            }

            ViewModel.ItemSource_KindOfSorting = new ObservableCollection<CMBType_String>
            {
             new CMBType_String(Properties.Resources.sort_by_date, "date"),
             new CMBType_String(Properties.Resources.sort_by_alphabet, "alpha" )
            };
            KindOfSortingPicker.ItemsSource = ViewModel.ItemSource_KindOfSorting.ToObservableCollection();

            var _selectedItem_KindOfSorting_key_name = Preferences.Get(ViewModel.SelectedItem_KindOfSorting_key_name, "date");  //, ViewModel.SelectedItem_KindOfSorting.Value);
            if (!string.IsNullOrEmpty(_selectedItem_KindOfSorting_key_name))
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == _selectedItem_KindOfSorting_key_name);
            else
                ViewModel.SelectedItem_KindOfSorting = ViewModel.ItemSource_KindOfSorting.FirstOrDefault(x => x.Value == "date");
            KindOfSortingPicker.SelectedIndex = ViewModel.ItemSource_KindOfSorting.IndexOf(ViewModel.SelectedItem_KindOfSorting);

            ViewModel.IsBusy=false;
        }

        protected override void OnAppearing()
        {
            string tmp_lists = Properties.Resources.Lists.ToUpper();

            ViewModel.Label_lists = tmp_lists;
            ViewModel.Label_Positions =  Properties.Resources.Positions.ToUpper();
        }

        private void SetCurrentCulture(string curr_culture)
        {
            //CultureInfo ci = new CultureInfo("en");
            var ci = new CultureInfo(curr_culture);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        #region HEADER

        private async void OnAddHeaderClicked(object sender, EventArgs e)
        {
            string new_list_name = await DisplayPromptAsync(Properties.Resources.Input, Properties.Resources.Enter_new_list_name, "OK", Properties.Resources.Cancel);
            if (!string.IsNullOrWhiteSpace(new_list_name))
            {
                var new_id = await ViewModel.AddHeader(new_list_name);
                ViewModel.SetFirstItemForHeaders();
            }
        }

        private async void OnHeaderSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ViewModel.IsBusy=true;

            ViewModel.HeaderSelected=true;

            var _selectedHeader = e.SelectedItem as Header;
            if (_selectedHeader != null)
            {
                ViewModel.Header_SelectedItem= _selectedHeader;

                await ViewModel.LoadPositions();

                expander.IsExpanded= !expander.IsExpanded;
                expander.IsExpanded= !expander.IsExpanded;

                //PositionsListHeight=newValue ? 400 : 600;
            }

            ViewModel.IsBusy=false;

            //ViewModel.HeaderSelected=false;
        }

        private async void OnDeleteHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                bool answer = await DisplayAlert(Properties.Resources.Delete_list, Properties.Resources.Are_you_sure, Properties.Resources.yes, Properties.Resources.no);
                if (answer)
                {
                    await ViewModel.DeleteHeader(header);
                    ViewModel.SetFirstItemForHeaders();

                    expander.IsExpanded= !expander.IsExpanded;
                    expander.IsExpanded= !expander.IsExpanded;
                }
            }
        }

        private async void OnCopyHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync(Properties.Resources.copy_list, Properties.Resources.Enter_list_name, "OK", Properties.Resources.Cancel);
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    var new_int = await ViewModel.CopyHeader(header, new_list_name);
                    ViewModel.SetFirstItemForHeaders();

                    expander.IsExpanded= !expander.IsExpanded;
                    expander.IsExpanded= !expander.IsExpanded;

                }
            }
        }

        private async void OnEditHeaderClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is Header header)
            {
                string new_list_name = await DisplayPromptAsync(Properties.Resources.Input, Properties.Resources.Enter_new_list_name, "OK", Properties.Resources.Cancel, initialValue: header.ListName);
                if (!string.IsNullOrWhiteSpace(new_list_name))
                {
                    await ViewModel.EditNameHeader(header, new_list_name);

                    expander.IsExpanded= !expander.IsExpanded;
                    expander.IsExpanded= !expander.IsExpanded;
                }
            }
        }



        #endregion


        #region POSITIONS

        private async void OnAddPositionClicked(object sender, EventArgs e)
        {
            //var text = await SpeechToText.Default.ListenAsync(new CultureInfo().def, null, CancellationToken.None);

            ViewModel.IsBusy=true;
            List<Position>? json= null;

            var promptPage = new InputTextWithMicrophone();  //_speechToText);
            await Navigation.PushModalAsync(promptPage);

            string new_item_name = await promptPage.Result;

            if (string.IsNullOrEmpty(new_item_name))
                return;

            try
            {
                json = JsonConvert.DeserializeObject<List<Position>>(new_item_name);
            }
            catch { json=null; }

            if (json != null)
            {
                foreach (var item in json)
                {
                    item.HeaderId= ViewModel.Header_SelectedItem.Id;
                    item.UpdatedAt= DateTime.Now;
                    await ViewModel.AddPosition(item);
                }
            }
            else
            {
                var new_pos = new Position();
                new_pos.HeaderId= ViewModel.Header_SelectedItem.Id;
                new_pos.Title= new_item_name;
                new_pos.UpdatedAt= DateTime.Now;

                if (new_item_name.Contains(",,"))  // && !ViewModel.Replace_old_word_when_inserting)
                {
                    var items_list = new_item_name.Split(",,").ToList();
                    if (items_list.Count > 0)
                    {
                        foreach (var item in items_list)
                        {
                            new_pos.Title= item.Trim();
                            await ViewModel.AddPosition(new_pos);
                        }
                    }
                    else
                        await ViewModel.AddPosition(new_pos);
                }
                else
                    await ViewModel.AddPosition(new_pos);
            }

            ////string new_item_name = await DisplayPromptAsync(Properties.Resources.Input, "Enter new item:", initialValue: result);
            //if (!string.IsNullOrWhiteSpace(new_item_name))
            //{
            //    if (new_item_name.Contains(",,"))
            //    {
            //        var items_list = new_item_name.Split(",,").ToList();
            //        if (items_list.Count > 0)
            //        {
            //            foreach (var item in items_list)
            //            {
            //                await ViewModel.AddPosition(item.Trim());
            //            }
            //        }
            //        else
            //            await ViewModel.AddPosition(new_item_name);
            //    }
            //    else
            //        await ViewModel.AddPosition(new_item_name);
            //}

            expander.IsExpanded= !expander.IsExpanded;
            expander.IsExpanded= !expander.IsExpanded;

            ViewModel.IsBusy=false;
        }

        private async void OnPositionToggled(object sender, ToggledEventArgs e)
        {
            if (ViewModel.IsBusy) return;
            if (ViewModel.HeaderSelected)
            {
                ViewModel.HeaderSelected=false;
                return;
            }

            //ViewModel.IsBusy=true;

            if (sender is Microsoft.Maui.Controls.Switch switchControl &&
                e !=null  &&
                switchControl.BindingContext != null &&
                switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = e.Value;
                await ViewModel.UpdatePosition(position);

                ViewModel.IsBusy = false;
            }

            //ViewModel.IsBusy=false;
        }

        //private void OnPositionSelected(object sender, SelectedItemChangedEventArgs e)
        //{
        //    ViewModel.Position_selectedItem = e.SelectedItem as Position;
        //}

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
                ViewModel.HeaderSelected=false;
                return;
            }

            //ViewModel.IsBusy=true;

            if (sender is Microsoft.Maui.Controls.CheckBox switchControl &&
                e !=null  &&
                switchControl.BindingContext != null &&
                switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = e.Value;
                position.UpdatedAt= DateTime.Now;
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

            if (sender is Microsoft.Maui.Controls.ImageButton switchControl &&
                e !=null  &&
                switchControl.BindingContext != null &&
                switchControl.BindingContext is Position position)
            {
                ViewModel.IsBusy = true;

                position.IsCompleted = !position.IsCompleted;  // e.Value;
                position.UpdatedAt= DateTime.Now;
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
    }

}
