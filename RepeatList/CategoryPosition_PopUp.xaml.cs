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

    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        await CloseAsync("");
    }

    private async void OkButton_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.SelectedCategory != null && !string.IsNullOrEmpty(ViewModel.SelectedCategory.Category))
            await CloseAsync(ViewModel.SelectedCategory.Category);
        else
            await CloseAsync("");
    }




}
