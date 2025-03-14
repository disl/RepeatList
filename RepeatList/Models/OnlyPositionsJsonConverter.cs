using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace RepeatList.Models
{
    public class OnlyPositionsJsonConverter : JsonConverter<Header>
    {
        public override void WriteJson(JsonWriter writer, Header value, JsonSerializer serializer)
        {
            writer.WriteStartObject(); // Beginnt ein JSON-Objekt
            writer.WritePropertyName("Id");
            serializer.Serialize(writer, value.Id);
            writer.WritePropertyName("ListName");
            serializer.Serialize(writer, value.ListName);
            writer.WritePropertyName("UpdatedAt");
            serializer.Serialize(writer, value.UpdatedAt);
            writer.WritePropertyName("Positions");
            serializer.Serialize(writer, value.Positions);  // **Nur `Positions` serialisieren**
            writer.WriteEndObject(); // Beendet das JSON-Objekt
        }

        public override Header ReadJson(JsonReader reader, Type objectType, Header existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var header = new Header
            {
                Id = jsonObject["Id"]?.ToString(),
                ListName = jsonObject["ListName"]?.ToString(),
                UpdatedAt = jsonObject["UpdatedAt"] != null
                    ? DateTime.Parse(jsonObject["UpdatedAt"].ToString(), CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind)
                    : DateTime.MinValue,
                Positions = jsonObject["Positions"]?.ToObject<List<Position>>() ?? new List<Position>()
            };

            return header;
        }

        //public override bool CanRead => false; // **Nur Serialisierung erlaubt**
    }
}
