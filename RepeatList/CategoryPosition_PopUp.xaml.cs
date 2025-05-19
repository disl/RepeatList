using CommunityToolkit.Maui.Views;
using RepeatList.ViewModels;
using System.Collections.ObjectModel;

namespace RepeatList;

public partial class CategoryPosition_PopUp : Popup
{
    private CategoryPosition_PopUpViewModel ViewModel { get; set; }

    public CategoryPosition_PopUp()
    {
        InitializeComponent();
    }

    public CategoryPosition_PopUp(ObservableCollection<string> list, string category)
    {
        InitializeComponent();

        ViewModel = new CategoryPosition_PopUpViewModel(list);
        BindingContext = ViewModel;

        if (category != null)
        {
            ViewModel.SelectedCategory = category;
        }
    }

    private void CancelButtonClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void OkButton_Clicked(object sender, EventArgs e)
    {
        Close(ViewModel.SelectedCategory);
    }
   
}
