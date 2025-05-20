using CommunityToolkit.Mvvm.ComponentModel;
using RepeatList.Models;
using RepeatList.Services;
using System.Collections.ObjectModel;

namespace RepeatList.ViewModels
{
    public partial class CategoryPosition_PopUpViewModel : ObservableObject
    {
        private DatabaseService _databaseService;

        [ObservableProperty] ObservableCollection<CategoryPosition_PopUpViewModelType> categories_colors_list = new();
        [ObservableProperty] string? title = "Select category";
        [ObservableProperty] CategoryPosition_PopUpViewModelType? selectedCategory;
        [ObservableProperty] List<CategoryPosition> categories_db;

        public CategoryPosition_PopUpViewModel()
        {
            _databaseService = new DatabaseService();
        }

        public CategoryPosition_PopUpViewModel(ObservableCollection<string> categories_list, string category)
        {
            _databaseService = new DatabaseService();

            for (int i=0; i < categories_list.Count; i++)
            {
                Categories_colors_list.Add(
                    new CategoryPosition_PopUpViewModelType(
                        categories_list[i], 
                        PositionsPageViewModel.ColorsList[i])
                    );
            }

            SelectedCategory = Categories_colors_list.FirstOrDefault(x => x.Category == category);

            //Categories = categories_list;
        }

        public async Task UpdateOrAdd(string Position, string Category)
        {
          if(string.IsNullOrEmpty(Position) || string.IsNullOrEmpty(Category))
                return;

            var item = await _databaseService.GetCategoryPositionAsync(Position);
            if(item == null)
            {
                await Add(Position, Category);
            }
            else
            {
                await Update(new CategoryPosition(Position, Category));
            }
            await FillList();
            SelectedCategory = Categories_colors_list.FirstOrDefault(x=>x.Category == Category);
        }

        public async Task<int> Add(string Position, string Category)
        {
            var newItem = new CategoryPosition (Position, Category);
            var new_id = await _databaseService.AddCategoryPositionAsync(newItem);

            await FillList();
            SelectedCategory = Categories_colors_list.FirstOrDefault(x => x.Category == Category);

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
            SelectedCategory = Categories_colors_list.FirstOrDefault(x => x.Category == item.Category);
        }

        public async Task FillList()
        {
            Categories_db = await _databaseService.GetCategoryPositionsAsync();
        }
    }

    public class CategoryPosition_PopUpViewModelType
    {
        public CategoryPosition_PopUpViewModelType(string category, Color color)
        {
            Category=category;
            Color=color;
        }

        public string Category { get; set; }
        public Color Color { get; set; }


    }
}
