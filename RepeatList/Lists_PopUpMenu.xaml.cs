using CommunityToolkit.Maui.Views;
using RepeatList.Models;

namespace RepeatList;

public partial class Lists_PopUpMenu : Popup<string>
{
    public Header SelectedItem { get; set; }

    public Lists_PopUpMenu()
    {
        InitializeComponent();
    }

    public Lists_PopUpMenu(Header item, bool SupabaseService_ready)
    {
        InitializeComponent();

        SelectedItem = item;

        Sync_removeButton.IsVisible = item.IsSynchronized && SupabaseService_ready;
        //Synchronisation_of_listsLabel.IsVisible = SupabaseService_ready;
        //Send_synchronisationButton.IsVisible = SupabaseService_ready;
    }

    private async void OnEditHeaderClicked(object sender, EventArgs e)
    {
       await CloseAsync("Edit");
    }

    private async void OnDeleteHeaderClicked(object sender, EventArgs e)
    {
        await CloseAsync("Delete");
    }

    private async void Sync_list_upClicked(object sender, EventArgs e)
    {
        await CloseAsync("SyncUp");
    }

    private async void Sync_removeClicked(object sender, EventArgs e)
    {
        await CloseAsync("SyncDelete");
    }

    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        await CloseAsync("");
    }

    private async void Export_list_Clicked(object sender, EventArgs e)
    {
       await CloseAsync("Export");
    }

    private async void Import_list_Clicked(object sender, EventArgs e)
    {
        await CloseAsync("Import");
    }
}
