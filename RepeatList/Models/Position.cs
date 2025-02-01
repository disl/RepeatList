

namespace RepeatList.Models
{
    public class Position
    {
     
        public int Id { get; set; }
        public int HeaderId { get; set; } // Fremdschlüssel zum Header
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
    }
}
