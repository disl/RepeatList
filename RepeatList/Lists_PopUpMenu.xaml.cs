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
        //await CloseAsync("Edit");
        await CloseMe("Edit");
    }

    private async void OnDeleteHeaderClicked(object sender, EventArgs e)
    {
        //await CloseAsync("Delete");
        await CloseMe("Delete");
    }

    private async void Sync_list_upClicked(object sender, EventArgs e)
    {
        //await CloseAsync("SyncUp");
        await CloseMe("SyncUp");
    }

    private async void Sync_removeClicked(object sender, EventArgs e)
    {
        //await CloseAsync("SyncDelete");
        await CloseMe("SyncDelete");
    }

    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        //await CloseAsync("");
        await CloseMe("");
    }

    private async void Export_list_Clicked(object sender, EventArgs e)
    {
        //await CloseAsync("Export");
        await CloseMe("Export");
    }

    private async void Import_list_Clicked(object sender, EventArgs e)
    {
        //await CloseAsync("Import");
        await CloseMe("Import");
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
