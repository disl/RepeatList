using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Models;
using RepeatList.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.ViewModels
{
    public partial class SetupPageViewModel : ObservableObject   // INotifyPropertyChanged,
    {
        #region PROPERTIES

        private DatabaseService _databaseService;
        public event PropertyChangedEventHandler PropertyChanged;

        [ObservableProperty] public ObservableCollection<Setup> list = new ObservableCollection<Setup>();
        [ObservableProperty] public Setup? selectedItem;

        #endregion

        //public double ButtonsSize = 30;

        public SetupPageViewModel()
        {
            _databaseService = new DatabaseService();
            _= Load();
        }

        public async Task Load()
        {
            var _list = await _databaseService.GetSetupsAsync();
            if (_list == null)
                return;

            if(List == null)
                List = new ObservableCollection<Setup>();
            List.Clear();
            List = new ObservableCollection<Setup>(_list);

            if (List != null &&  List.Count > 0)
            {
                SelectedItem = List.FirstOrDefault();
            }
        }

        public async Task<int> Add(string DefaultLanguage, string DefaultAppTheme)
        {
            var newItem = new Setup { DefaultLanguage = DefaultLanguage, DefaultAppTheme = DefaultAppTheme };
            var new_id = await _databaseService.AddSetupAsync(newItem);

            await Load();

            var selectedItem = await _databaseService.GetSetupAsync(new_id);
            SelectedItem = selectedItem;

            return new_id;
        }

        public async Task DeleteSetup(Models.Setup Setup)
        {
            if (Setup == null) return;

            SelectedItem = Setup;
            await _databaseService.DeleteSetupAsync(Setup.Id);
            await Load();
        }


        //[ObservableProperty] private string imageButton_size = 35; 
        //[ObservableProperty] private string defaultLanguage;
        //[ObservableProperty] private string defaultAppTheme;




    }
}
