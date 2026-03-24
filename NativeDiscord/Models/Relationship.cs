using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace NativeDiscord.Models
{
    public class Relationship : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; } // 1 = Friend, 2 = Blocked, 3 = Incoming, 4 = Outgoing

        [JsonPropertyName("user")]
        public User User { get; set; }

        private List<Activity> _activities;
        public List<Activity> Activities
        {
            get => _activities;
            set
            {
                if (_activities != value)
                {
                    _activities = value;
                    OnPropertyChanged(nameof(Activities));
                    OnPropertyChanged(nameof(ActivityText));
                    OnPropertyChanged(nameof(HasActivity));
                    OnPropertyChanged(nameof(ActivityVisibility));
                    OnPropertyChanged(nameof(PrimaryActivity));
                    OnPropertyChanged(nameof(RichPresenceCardVisibility));

                    // Notify Safe Proxies
                    OnPropertyChanged(nameof(ActivityName));
                    OnPropertyChanged(nameof(ActivityDetails));
                    OnPropertyChanged(nameof(ActivityState));
                    OnPropertyChanged(nameof(ActivityHeaderIconUrl));
                    OnPropertyChanged(nameof(ActivityPrimaryImageUrl));
                    OnPropertyChanged(nameof(ActivitySecondaryImageUrl));
                    OnPropertyChanged(nameof(HasActivityPrimaryImage));
                    OnPropertyChanged(nameof(HasActivitySecondaryImage));
                }
            }
        }

        public Activity PrimaryActivity => (Activities != null && Activities.Count > 0) ? Activities[0] : null;

        // Safe Proxy Properties for UI Binding to avoid NullReference/ArgumentException in x:Bind
        public string ActivityName => PrimaryActivity?.Name ?? "";
        public string ActivityDetails => PrimaryActivity?.Details ?? "";
        public string ActivityState => PrimaryActivity?.State ?? "";

        public string ActivityHeaderIconUrl => PrimaryActivity?.HeaderIconUrl ?? null;

        public string ActivityPrimaryImageUrl => PrimaryActivity?.PrimaryImageUrl; // Returns null if PrimaryActivity is null
        public string ActivitySecondaryImageUrl => PrimaryActivity?.SecondaryImageUrl; // Returns null if PrimaryActivity is null

        public Visibility HasActivityPrimaryImage => (PrimaryActivity != null && !string.IsNullOrEmpty(PrimaryActivity.PrimaryImageUrl)) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HasActivitySecondaryImage => (PrimaryActivity != null && !string.IsNullOrEmpty(PrimaryActivity.SecondaryImageUrl)) ? Visibility.Visible : Visibility.Collapsed;


        private string _elapsedTime = "";
        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                if (_elapsedTime != value)
                {
                    _elapsedTime = value ?? "";
                    OnPropertyChanged(nameof(ElapsedTime));
                    OnPropertyChanged(nameof(ElapsedTimeVisibility));
                }
            }
        }

        public Visibility ElapsedTimeVisibility => !string.IsNullOrEmpty(ElapsedTime) ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper properties for UI binding
        private string _status = "offline";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(IsOnline));
                }
            }
        }

        // Keep IsOnline for backward compatibility or simple checks
        public bool IsOnline => Status != "offline" && Status != "invisible";

        public string StatusText
        {
            get
            {
                if (Type == 2) return "Blocked";
                if (Type == 3) return "Incoming Friend Request";
                if (Type == 4) return "Outgoing Friend Request";

                if (HasActivity) return ActivityText;

                // Capitalize status
                if (string.IsNullOrEmpty(Status)) return "Offline";
                return char.ToUpper(Status[0]) + Status.Substring(1);
            }
        }

        public Color StatusColor
        {
             get
             {
                 string hex = "#747F8D"; // Default Offline

                 if (Type == 2) hex = "#ED4245"; // Red for blocked
                 else
                 {
                     switch (Status)
                     {
                         case "online": hex = "#23A559"; break;
                         case "idle": hex = "#F0B232"; break;
                         case "dnd": hex = "#F23F43"; break;
                         default: hex = "#747F8D"; break;
                     }
                 }

                 return GetColorFromHex(hex);
             }
        }

        private Color GetColorFromHex(string hex)
        {
            try
            {
                hex = hex.Replace("#", "");
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
            }
            catch { }
            return Color.FromArgb(255, 128, 128, 128); // Fallback Gray
        }


        public string ActivityText
        {
            get
            {
                if (Activities != null && Activities.Count > 0)
                {
                    var activity = Activities[0];
                    switch (activity.Type)
                    {
                        case 0: return $"Playing {activity.Name}";
                        case 1: return $"Streaming {activity.Name}";
                        case 2: return $"Listening to {activity.Name}";
                        case 3: return $"Watching {activity.Name}";
                        case 4: // Custom Status
                            return !string.IsNullOrEmpty(activity.State) ? activity.State : activity.Name;
                        case 5: return $"Competing in {activity.Name}";
                        default: return activity.Name;
                    }
                }
                return null;
            }
        }

        public bool HasActivity => !string.IsNullOrEmpty(ActivityText);

        public Visibility ActivityVisibility => HasActivity ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RichPresenceCardVisibility =>
            (PrimaryActivity != null && PrimaryActivity.IsRichPresence && PrimaryActivity.Type != 4) ? Visibility.Visible : Visibility.Collapsed;
    }
}
