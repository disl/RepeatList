using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Models;

namespace RepeatList.ViewModels
{
    public partial class Positions_EditViewModel : ObservableObject
    {

        [ObservableProperty]
        public Position selectedItem;

       

    }
}