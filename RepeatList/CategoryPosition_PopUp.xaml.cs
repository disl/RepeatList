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
        // Popup zuerst schlieﬂen
        if (Handler != null)
        {
            CloseAsync(param); // Bei Popup<TResult> -> kein await, sofort Ergebnis setzen
        }

        // Danach Navigation
        if (Navigation.ModalStack.Any())
        {
            await Navigation.PopModalAsync();
        }

        //if (!Navigation.ModalStack.Any())
        //{
        //    await CloseAsync(param);
        //}
        //else
        //{
        //    await Navigation.PopModalAsync();
        //    await CloseAsync(param);
        //}
    }
}



