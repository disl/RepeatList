using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RepeatList.Models
{
    [Table("subscriptions")]
    public class Subscription : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Column("is_premium")]
        public bool IsPremium { get; set; }

        [Column("purchase_token")]
        public string? PurchaseToken { get; set; }

        [Column("product_id")]
        public string? ProductId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
