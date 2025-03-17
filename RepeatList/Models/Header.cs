using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
//using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Header : BaseModel
    {
        //[System.ComponentModel.DataAnnotations.Key]
        [PrimaryKey]
        //[JsonIgnore]
        public string Id { get; set; }
        public string ListName { get; set; }

        [NotMapped]
        [IgnoreDataMember]
        public string AvatarName
        {
            get
            {
                if (string.IsNullOrEmpty(ListName)) return "";
                else
                    return ListName.Substring(0, 2).ToUpper();
            }
        }
        public DateTime UpdatedAt { get; set; }

        //[JsonIgnore]
        //[NotMapped]
        //[IgnoreDataMember]
        public bool IsSynchronized { get; set; } = false;

        [NotMapped]
        [IgnoreDataMember]
        public string Sync_arrow_down_icon
        {
            get
            {
                if (!IsSynchronized)
                    return "";
                else
                {
                    return IsSupabaseOk ? "sync_arrow_down_icon_green.png" : "sync_arrow_down_icon_red.png";
                }
            }
        }

        [NotMapped]
        [IgnoreDataMember]
        public static bool IsSupabaseOk { get; set; }

        [NotMapped]
        [IgnoreDataMember]
        public List<Position> Positions { get; set; } = new();
    }

}
