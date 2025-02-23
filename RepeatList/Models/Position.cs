using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;
//using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Position : BaseModel
    {
        //[JsonIgnore]

        [System.ComponentModel.DataAnnotations.Key]
        [PrimaryKey]
        public string Id { get; set; }
        //[JsonIgnore]
        public string HeaderId { get; set; }  
      
        public string? Title { get; set; }
        //[JsonIgnore]
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        [NotMapped]
        //[IgnoreDataMember]
        public string PositionImageSource
        {
            get
            {
                string image_source = "";
                if (Application.Current.UserAppTheme == AppTheme.Dark)
                {
                    image_source= IsCompleted ? "check_box_check_white.png" : "check_box_blank_white.png";
                }
                else
                {
                    image_source= IsCompleted ? "check_box_check.png" : "check_box_blank.png";
                }

                return image_source;
            }
        }
        public bool IsCompleted { get; set; } = false;
    }
}
