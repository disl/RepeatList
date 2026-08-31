using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace RepeatList.Models
{
    public class Header : BaseModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        //[System.ComponentModel.DataAnnotations.Key]
        [PrimaryKey]
        //[JsonIgnore]
        public string Id { get; set; }

        private string _listName = string.Empty;

        public string ListName
        {
            get => _listName;
            set
            {
                if (_listName != value)
                {
                    _listName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AvatarName));
                }
            }
        }

        [NotMapped]
        [IgnoreDataMember]
        public string AvatarName
        {
            get
            {
                if (string.IsNullOrEmpty(ListName) || ListName.Length < 2) return "";
                else
                    return ListName.Substring(0, 2).ToUpper();
            }
        }

        public DateTime UpdatedAt { get; set; }

        //[JsonIgnore]
        //[NotMapped]
        //[IgnoreDataMember]
        private bool _isSynchronized = false;

        public bool IsSynchronized
        {
            get => _isSynchronized;
            set
            {
                if (_isSynchronized != value)
                {
                    _isSynchronized = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Sync_arrow_down_icon));
                }
            }
        }

        [NotMapped]
        [IgnoreDataMember]
        public string Sync_arrow_down_icon
        {
            get
            {
                if (!IsSynchronized)
                    return "";
                else
                {
                    return IsSupabaseOk ? "icon_sync_green.png" : "icon_sync_red.png";
                }
            }
        }

        // Pro-Header Zustand der letzten Supabase-Synchronisation. Früher static:
        // ein Header hat alle anderen eingefärbt und das Setzen löste kein UI-Update aus.
        [NotMapped]
        [IgnoreDataMember]
        private bool _isSupabaseOk;

        [NotMapped]
        [IgnoreDataMember]
        public bool IsSupabaseOk
        {
            get => _isSupabaseOk;
            set
            {
                if (_isSupabaseOk != value)
                {
                    _isSupabaseOk = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Sync_arrow_down_icon));
                }
            }
        }

        [NotMapped]
        [IgnoreDataMember]
        public List<Position> Positions { get; set; } = new();
    }

}
