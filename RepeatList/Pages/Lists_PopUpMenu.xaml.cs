using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.Services;

namespace RepeatList;

public partial class Lists_PopUpMenu : Popup<string>
{
    public Header SelectedItem { get; set; }
    private readonly SpotifyService _spotifyService = new SpotifyService();

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

    private async void Export_text_list_Clicked(object sender, EventArgs e)
    {
        //await CloseAsync("Export");
        await CloseMe("Export_text");
    }

    private async void Import_list_Clicked(object sender, EventArgs e)
    {
        //await CloseAsync("Import");
        await CloseMe("Import");
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

    private async void Export_list_spotifyClicked(object sender, EventArgs e)
    {
       await CloseAsync("ExportSpotify");
    }

    private async void Import_list_file_Clicked(object sender, EventArgs e)
    {
        await CloseAsync("Import_list_file");
    }
}
