using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Emoji
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("animated")]
        public bool Animated { get; set; }

        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(Id))
                {
                    return null;
                }
                string format = Animated ? "gif" : "png";
                return $"https://cdn.discordapp.com/emojis/{Id}.{format}";
            }
        }

        public bool IsCustom => !string.IsNullOrEmpty(Id);
    }

    public class Reaction : INotifyPropertyChanged
    {
        private int _count;
        [JsonPropertyName("count")]
        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        private bool _me;
        [JsonPropertyName("me")]
        public bool Me
        {
            get => _me;
            set
            {
                if (_me != value)
                {
                    _me = value;
                    OnPropertyChanged(nameof(Me));
                }
            }
        }

        [JsonPropertyName("emoji")]
        public Emoji Emoji { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
