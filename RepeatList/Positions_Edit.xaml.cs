using CommunityToolkit.Maui.Views;
using RepeatList.Models;
using RepeatList.ViewModels;

namespace RepeatList;

public partial class Positions_Edit : Popup
{

    public Positions_EditViewModel ViewModel { get; set; }

    public Positions_Edit(Position? position_selectedItem)
    {
        InitializeComponent();

        ViewModel = new Positions_EditViewModel();
        ViewModel.SelectedItem = position_selectedItem;
        BindingContext = ViewModel;
    }

    //protected override void OnParentSet()
    //{
    //    base.OnParentSet();

    //    //Export_not_completed_as_a_text_list_Button.InvalidateMeasure();

    //    //// Set the default value for the checkbox based on the saved preference
    //    //bool isChecked = Preferences.Get("duplicate_entries_add", true);
    //    //rbDuplicate_entries_add.IsChecked = isChecked;
    //    //rbDuplicate_entries_replace.IsChecked = !isChecked;
    //}

    private void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        Close("delete");
    }


    private void CancelButtonClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void OnSavePositionTitle_Clicked(object sender, EventArgs e)
    {
        PositionNameEntry.Unfocus();

        Close(ViewModel.SelectedItem);
    }
}
