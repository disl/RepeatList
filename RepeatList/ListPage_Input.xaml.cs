using CommunityToolkit.Maui.Views;

namespace RepeatList;

public partial class ListPage_Input : Popup
{
    public ListPage_Input()
	{
		InitializeComponent();
	}

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnOkClicked(object sender, EventArgs e)
    {
        string input = ListNameEditor.Text?.Trim();
        Close(input);
    }
}