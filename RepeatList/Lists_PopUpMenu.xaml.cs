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

    private void OnEditHeaderClicked(object sender, EventArgs e)
    {
       CloseAsync("Edit");
    }

    private void OnDeleteHeaderClicked(object sender, EventArgs e)
    {
        CloseAsync("Delete");
    }

    private void Sync_list_upClicked(object sender, EventArgs e)
    {
        CloseAsync("SyncUp");
    }

    private void Sync_removeClicked(object sender, EventArgs e)
    {
        CloseAsync("SyncDelete");
    }

    private void CancelButtonClicked(object sender, EventArgs e)
    {
        CloseAsync("");
    }

    private void Export_list_Clicked(object sender, EventArgs e)
    {
        CloseAsync("Export");
    }

    private void Import_list_Clicked(object sender, EventArgs e)
    {
        CloseAsync("Import");
    }
}
