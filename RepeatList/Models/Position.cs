

namespace RepeatList.Models
{
    public class Position
    {

        public int Id { get; set; }
        public int HeaderId { get; set; } // Fremdschlüssel zum Header
        public string? Title { get; set; }

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
