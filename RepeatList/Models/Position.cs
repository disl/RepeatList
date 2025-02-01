using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.Models
{
    public class Position
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int HeaderId { get; set; } // Fremdschlüssel zum Header
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
    }
}
