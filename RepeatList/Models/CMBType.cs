namespace RepeatList.Models
{
    public class CMBType_String
    {
        public CMBType_String(string name, string value)
        {
            Name=name;
            Value=value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
