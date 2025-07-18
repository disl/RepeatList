namespace RepeatList.Models
{
    public class ChatResponseType
    {
        public class Item
        {
            public string item { get; set; }
            public string quantity { get; set; }
        }

        public class Root
        {
            public string root { get; set; }
            public string thema { get; set; }
            public string description { get; set; }
            public List<Item> items { get; set; }
        }
    }
}
