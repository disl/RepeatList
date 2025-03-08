using Newtonsoft.Json;

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
            throw new NotImplementedException("Deserialisierung wird nicht unterstützt.");
        }

        public override bool CanRead => false; // **Nur Serialisierung erlaubt**
    }
}
