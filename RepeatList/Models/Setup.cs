using System.Globalization;

namespace RepeatList.Models
{
    public class Setup
    {
        public int Id { get; set; }
        public string DefaultLanguage { get; set; }=CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public string DefaultAppTheme { get; set; } = "Dark";
    }
}
