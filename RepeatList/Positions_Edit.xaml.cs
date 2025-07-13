using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class Positions_Edit : Popup<object>
{

    public Positions_EditViewModel ViewModel { get; set; }

    public Positions_Edit(Position? position_selectedItem)
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

   

    private void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        CloseAsync("delete");
    }


    private void CancelButtonClicked(object sender, EventArgs e)
    {
        CloseAsync();
    }

    private void OnSavePositionTitle_Clicked(object sender, EventArgs e)
    {
        PositionNameEntry.Unfocus();

        CloseAsync(ViewModel.SelectedItem);
    }
}
