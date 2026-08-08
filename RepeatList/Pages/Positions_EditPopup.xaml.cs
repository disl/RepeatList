using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class Positions_EditPopup : Popup<object>
{
    public Positions_EditViewModel ViewModel { get; set; }

    public Positions_EditPopup(Position? position_selectedItem)
    {
        InitializeComponent();

        ViewModel = new Positions_EditViewModel();
        ViewModel.SelectedItem = position_selectedItem;
        BindingContext = ViewModel;

        Device.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100); // Kleine Verzögerung, falls UI noch nicht bereit
            PositionNameEntry.Focus();
            PositionNameEntry.CursorPosition= PositionNameEntry.Text?.Length ?? 0; 
        });
    }

   

    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        //await CloseAsync("delete");
        await CloseMe("delete");
    }


    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        //await CloseAsync(null);
        await CloseMe(null);
    }

    private async void OnSavePositionTitle_Clicked(object sender, EventArgs e)
    {
        PositionNameEntry.Unfocus();

        //await CloseAsync(ViewModel.SelectedItem);
        await CloseMe(ViewModel.SelectedItem);

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
