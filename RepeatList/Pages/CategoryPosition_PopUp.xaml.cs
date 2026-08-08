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
        // await CloseAsync("");
        await CloseMe("");
    }

    private async void OkButton_Clicked(object sender, EventArgs e)
    {
        dynamic ret_val;

        if (ViewModel.SelectedCategory != null && !string.IsNullOrEmpty(ViewModel.SelectedCategory.Category))
            ret_val = ViewModel.SelectedCategory.Category;
        else
            ret_val = "";

        //await CloseAsync(ret_val);
        await CloseMe(ret_val);
    }

    async Task CloseMe(dynamic param)
    {
        try
        {
            // Popup<TResult>.CloseAsync(param) sets the result and closes the popup.
            await CloseAsync(param);
        }
        catch (InvalidOperationException) when (Navigation.ModalStack.Any())
        {
            // CommunityToolkit.Maui throws PopupBlockedException (internal) when another
            // modal (e.g. the AI-unlock popup) is on top of the modal stack. Pop the
            // topmost modal and try closing again.
            await Navigation.PopModalAsync();
            await CloseAsync(param);
        }
    }
}



