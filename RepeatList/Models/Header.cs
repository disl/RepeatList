using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Header : BaseModel
    {
        //[System.ComponentModel.DataAnnotations.Key]
        //[PrimaryKey]
        //[JsonIgnore]
        public string Id { get; set; }
        public string ListName { get; set; }
        public DateTime UpdatedAt { get; set; }

        //[JsonIgnore]
        [NotMapped]
        //[IgnoreDataMember]
        public bool IsSynchronized { get; set; } = false;

        public List<Position> Positions { get; set; } = new();
    }

}
