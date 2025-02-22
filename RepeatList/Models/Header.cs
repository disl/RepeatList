

using Newtonsoft.Json;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Header : BaseModel
    {
        public string Id { get; set; }
        public string ListName { get; set; }
        public DateTime UpdatedAt { get; set; }

        [JsonIgnore]
        [NotMapped]
        [IgnoreDataMember]
        public bool IsSynchronized { get; set; } = false;
    }

}
