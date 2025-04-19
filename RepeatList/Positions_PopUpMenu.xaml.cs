using CommunityToolkit.Maui.Views;
using RepeatList.Models;

namespace RepeatList;

public partial class Positions_PopUpMenu : Popup
{
    public Position SelectedItem { get; set; }

    public Positions_PopUpMenu()
    {
        InitializeComponent();
    }

    //public Positions_PopUpMenu(List<Position> item)
    //{
    //    InitializeComponent();

    //    SelectedItem = item;       
    //}

    protected override void OnParentSet()
    {
        base.OnParentSet();

        Export_not_completed_as_a_text_list_Button.InvalidateMeasure();

        // Set the default value for the checkbox based on the saved preference
        bool isChecked = Preferences.Get("duplicate_entries_add", false);
        rbDuplicate_entries_add.IsChecked = isChecked;
        rbDuplicate_entries_replace.IsChecked = !isChecked;
    }


    private void OnExport_not_completed_as_a_text_listClicked(object sender, EventArgs e)
    {
        Close("Export_not_completed_as_a_text_list");
    }

    private void Duplicate_entries_replace(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("duplicate_entries_add", false);
    }

    private void Duplicate_entries_add(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("duplicate_entries_add", true);
    }

    private void CancelButtonClicked(object sender, EventArgs e)
    {
        Close();
    }
}
