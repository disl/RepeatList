using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class Positions_EditPopup : Popup
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
        await CloseAsync("delete");
    }


    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }

    private async void OnSavePositionTitle_Clicked(object sender, EventArgs e)
    {
        PositionNameEntry.Unfocus();

        await CloseAsync(ViewModel.SelectedItem);
    }
}
