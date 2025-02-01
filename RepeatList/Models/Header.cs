using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepeatList.Models
{
    public class Header
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ListName { get; set; }
        public DateTime Date { get; set; }
    }
}
