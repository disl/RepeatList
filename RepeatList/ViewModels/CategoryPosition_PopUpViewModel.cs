using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace RepeatList.ViewModels
{
    public partial class CategoryPosition_PopUpViewModel : ObservableObject
    {
        private DatabaseService _databaseService;

        [ObservableProperty] string? title = "Select category";
        [ObservableProperty] string? selectedCategory;
        [ObservableProperty] ObservableCollection<string> categories = new ObservableCollection<string>();
        [ObservableProperty] List<CategoryPosition> categories_db;

        public CategoryPosition_PopUpViewModel()
        {
            _databaseService = new DatabaseService();
        }

        public CategoryPosition_PopUpViewModel(ObservableCollection<string> categories_list)
        {
            _databaseService = new DatabaseService();

            Categories = categories_list;
        }

        public async Task UpdateOrAdd(string Position, string Categorie)
        {
          if(string.IsNullOrEmpty(Position) || string.IsNullOrEmpty(Categorie))
                return;

            var item = await _databaseService.GetCategoryPositionAsync(Position);
            if(item == null)
            {
                await Add(Position, Categorie);
            }
            else
            {
                await Update(new CategoryPosition(Position, Categorie));
            }
            await FillList();
            SelectedCategory = Categorie;
        }

        public async Task<int> Add(string Position, string Categorie)
        {
            var newItem = new CategoryPosition (Position, Categorie);
            var new_id = await _databaseService.AddCategoryPositionAsync(newItem);

            await FillList();
            SelectedCategory = Categorie;

            return new_id;
        }

        public async Task Delete(CategoryPosition item)
        {
            if (item == null) return;

            await _databaseService.DeleteCategoryPositionAsync(item.Position);
            await FillList();
        }

        public async Task Update(CategoryPosition item)
        {
            if (item == null) return;

            await _databaseService.UpdateCategoryPositionAsync(item);
            await FillList();

            await FillList();
            SelectedCategory = item.Category;
        }

        public async Task FillList()
        {
            Categories_db = await _databaseService.GetCategoryPositionsAsync();
        }
    }
}
