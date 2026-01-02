using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace NativeDiscord.Models
{
    public class DMChannelViewModel : System.ComponentModel.INotifyPropertyChanged, IDisposable
    {
        private NativeDiscord.Services.DiscordService _service;
        private string _targetUserId;
        private bool _disposed;

        public Channel Channel { get; }
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public string Subtitle { get; set; } // Last message or status
        public Visibility SubtitleVisibility => string.IsNullOrEmpty(Subtitle) ? Visibility.Collapsed : Visibility.Visible;

        private Brush _statusColor = new SolidColorBrush(Color.FromArgb(255, 116, 127, 141)); // Default Gray
        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusColor)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public DMChannelViewModel(Channel channel, NativeDiscord.Services.DiscordService service = null)
        {
            Channel = channel;
            _service = service;

            // Logic to determine Name and Icon from Recipients
            if (channel.Recipients != null && channel.Recipients.Count > 0)
            {
                if (channel.Recipients.Count == 1)
                {
                    // 1-on-1 DM
                    var user = channel.Recipients[0];
                    _targetUserId = user.Id;
                    Name = user.DisplayName;
                    IconUrl = user.AvatarUrl;

                    // Initial Status Check
                    UpdateStatus();
                }
                else
                {
                    // Group DM
                    var names = new List<string>();
                    foreach (var r in channel.Recipients) names.Add(r.DisplayName);
                    Name = string.Join(", ", names);
                    IconUrl = "ms-appx:///Assets/DiscordLogo.png"; // Fallback for Group DM icon
                }
            }
            else
            {
                Name = !string.IsNullOrEmpty(channel.Name) ? channel.Name : "Direct Message";
                IconUrl = "ms-appx:///Assets/DiscordLogo.png"; // Default
            }

            if (_service != null)
            {
                _service.PresenceUpdated += Service_PresenceUpdated;
            }
        }

        ~DMChannelViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            // Always detach from long-lived service events to avoid leaking this VM.
            if (_service != null)
            {
                _service.PresenceUpdated -= Service_PresenceUpdated;
            }

            _service = null;
            _targetUserId = null;
        }

        private void UpdateStatus()
        {
            if (_service == null || string.IsNullOrEmpty(_targetUserId)) return;

            string status = _service.GetUserStatus(_targetUserId);

            Color color;
            switch (status)
            {
                case "online":
                    color = Color.FromArgb(255, 35, 165, 89); // Green
                    break;
                case "idle":
                    color = Color.FromArgb(255, 240, 178, 50); // Yellow (#F0B232)
                    break;
                case "dnd":
                    color = Color.FromArgb(255, 242, 63, 67); // Red (#F23F43)
                    break;
                default:
                    color = Color.FromArgb(255, 116, 127, 141); // Gray
                    break;
            }
            StatusColor = new SolidColorBrush(color);
        }

        private void Service_PresenceUpdated(object sender, PresenceUpdate e)
        {
            if (_disposed) return;

            if (e.User != null && e.User.Id == _targetUserId)
            {
                try
                {
                    if (App.MainWindow != null)
                    {
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (_disposed) return;
                            UpdateStatus();
                        });
                    }
                    else
                    {
                        UpdateStatus();
                    }
                }
                catch
                {
                    UpdateStatus();
                }
            }
        }
    }
}
