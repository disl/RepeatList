using RepeatList.ViewModels;
using System.Collections.ObjectModel;

namespace RepeatList;

public partial class CategoryPositionPage : ContentPage
{
    private CategoryPositionViewModel ViewModel { get; set; }

    public CategoryPositionPage(ObservableCollection<string> list)
	{
		InitializeComponent();

        ViewModel = new CategoryPositionViewModel(list);
        BindingContext = ViewModel;
    }
}