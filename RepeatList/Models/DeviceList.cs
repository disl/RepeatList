using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RepeatList.Models
{
    public class DeviceList : BaseModel
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string DeviceId { get; set; }

        public string Description { get; set; }

    }
}
