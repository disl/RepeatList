using CommunityToolkit.Maui.Views;
using RepeatList.Models;

namespace RepeatList;

public partial class Positions_PopUpMenu : Popup<string>
{
    public Position SelectedItem { get; set; }
    private bool _supabaseServiceReady;

    public Positions_PopUpMenu()
    {
        InitializeComponent();
    }

    public Positions_PopUpMenu(bool SupabaseService_ready)
    {
        InitializeComponent();

        _supabaseServiceReady = SupabaseService_ready;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        Export_not_completed_as_a_text_list_Button.InvalidateMeasure();

        // Set the default value for the checkbox based on the saved preference
        bool isChecked = Preferences.Get("duplicate_entries_add", true);
        rbDuplicate_entries_add.IsChecked = isChecked;
        rbDuplicate_entries_replace.IsChecked = !isChecked;
    }


    private async void OnExport_not_completed_as_a_text_listClicked(object sender, EventArgs e)
    {
        //await CloseAsync("Export_not_completed_as_a_text_list");
        await CloseMe("Export_not_completed_as_a_text_list");
    }

    private void Duplicate_entries_replace(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("duplicate_entries_add", false);
    }

    private void Duplicate_entries_add(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("duplicate_entries_add", true);
    }

    private async void CancelButtonClicked(object sender, EventArgs e)
    {
        //await CloseAsync();
        await CloseMe(null);
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
