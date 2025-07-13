using CommunityToolkit.Maui.Views;

namespace RepeatList;

public partial class ListPage_Input : Popup<string>
{
    public ListPage_Input()
	{
		InitializeComponent();
	}

    private void OnCancelClicked(object sender, EventArgs e)
    {
        CloseAsync("");
    }

    private void OnOkClicked(object sender, EventArgs e)
    {
        string input = ListNameEditor.Text?.Trim();
        CloseAsync(input);
    }
}