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
    }


    private void OnExport_not_completed_as_a_text_listClicked(object sender, EventArgs e)
    {
        Close("Export_not_completed_as_a_text_list");
    }
}
