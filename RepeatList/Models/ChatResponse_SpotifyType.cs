namespace RepeatList.Models
{
    public class ChatResponse_SpotifyType
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Header
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        public class Item
        {
            public string Title { get; set; }
            public string Artist { get; set; }
        }

        public class Root
        {
            public Header Header { get; set; }
            public List<Item> Items { get; set; }
        }


    }
}
