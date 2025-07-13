using CommunityToolkit.Maui.Views;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class CategoryPosition_PopUp : Popup<string>
{
    private CategoryPosition_PopUpViewModel ViewModel { get; set; }



    public CategoryPosition_PopUp()
    {
        InitializeComponent();
    }

    public CategoryPosition_PopUp(List<Categories_listType> list, string category)
    {
        InitializeComponent();

        ViewModel = new CategoryPosition_PopUpViewModel(list, category);
        BindingContext = ViewModel;
    }

    private void CancelButtonClicked(object sender, EventArgs e)
    {
        CloseAsync("");
    }

    private void OkButton_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.SelectedCategory != null && !string.IsNullOrEmpty(ViewModel.SelectedCategory.Category))
            CloseAsync(ViewModel.SelectedCategory.Category);
        else
            CloseAsync("");
    }




}
