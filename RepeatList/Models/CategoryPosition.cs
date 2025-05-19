using System.ComponentModel.DataAnnotations;

namespace RepeatList.Models
{
    public class CategoryPosition
    {
        public CategoryPosition() { }

        public CategoryPosition(string position, string category)
        {
            Position=position;
            Category=category;
        }

        [Key]
        public string Position { get; set; }
        public string Category { get; set; }
    }
}
