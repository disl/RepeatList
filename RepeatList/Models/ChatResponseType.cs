using Newtonsoft.Json;

namespace RepeatList.Models
{
    public class ChatResponseType
    {
        public partial class Root
        {
            [JsonProperty("Header")]
            public Header Header { get; set; }

            [JsonProperty("Items")]
            public Item[] Items { get; set; }
        }

        public partial class Header
        {
            [JsonProperty("Title")]
            public string Title { get; set; }

            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("Sequence_text")]
            public string SequenceText { get; set; }
        }

        public partial class Item
        {
            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("Quantity")]
            public string Quantity { get; set; }
        }
    }
}
