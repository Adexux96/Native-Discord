using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace NativeDiscord.Models
{
    public class User
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("username")]
        public string Username { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("global_name")]
        public string GlobalName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("avatar")]
        public string Avatar { get; set; } // Raw hash
        
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; }

        public string AvatarUrl 
        { 
            get 
            {
                if (string.IsNullOrEmpty(Avatar) || string.IsNullOrEmpty(Id))
                    return "https://cdn.discordapp.com/embed/avatars/0.png";
                return $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.png";
            } 
        }
        
        public string DisplayName => !string.IsNullOrEmpty(GlobalName) ? GlobalName : Username;
    }

    public class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; } // Raw hash

        public string IconUrl
        {
            get
            {
                if (string.IsNullOrEmpty(Icon))
                    return null; // Return null to use fallback or Initials
                return $"https://cdn.discordapp.com/icons/{Id}/{Icon}.png";
            }
        }

        [System.Text.Json.Serialization.JsonPropertyName("voice_states")]
        public List<VoiceState> VoiceStates { get; set; }
    }

    public class Channel
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public int Type { get; set; } // 0 = Text, 2 = Voice, 4 = Category
        
        [System.Text.Json.Serialization.JsonPropertyName("parent_id")]
        public string ParentId { get; set; } // Category ID
        
        [System.Text.Json.Serialization.JsonPropertyName("position")]
        public int Position { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("permission_overwrites")]
        public List<PermissionOverwrite> PermissionOverwrites { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("recipients")]
        public List<User> Recipients { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("user_limit")]
        public int UserLimit { get; set; }
    }

    public class PermissionOverwrite
    {
        public string Id { get; set; } // Role or User ID
        public int Type { get; set; } // 0 = Role, 1 = Member
        public string Allow { get; set; } // Bitmask
        public string Deny { get; set; } // Bitmask
    }

    public class GuildMember
    {
        public User User { get; set; }
        public List<string> Roles { get; set; }
    }

    public class Role
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Permissions { get; set; } // Bitmask
        public int Position { get; set; }
    }

    public class Message
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("edited_timestamp")]
        public DateTimeOffset? EditedTimestamp { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("author")]
        public User Author { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("attachments")]
        public List<Attachment> Attachments { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("embeds")]
        public List<Embed> Embeds { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("reactions")]
        public List<Reaction> Reactions { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("message_reference")]
        public MessageReference MessageReference { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("referenced_message")]
        public Message ReferencedMessage { get; set; }
        
        public string TimestampFormatted 
        {
            get
            {
                if (Timestamp.Date == DateTimeOffset.Now.Date)
                {
                    return Timestamp.ToString("t"); // Short time: 1:07 AM
                }
                return Timestamp.ToString("g"); // 12/28/2025 1:07 AM
            }
        }

        // Helpers for UI
        public bool HasAttachments => Attachments != null && Attachments.Count > 0;
        public bool HasEmbeds => Embeds != null && Embeds.Count > 0;
        public bool HasReactions => Reactions != null && Reactions.Count > 0;
    }

    public class Reaction : System.ComponentModel.INotifyPropertyChanged
    {
        private int _count;
        [System.Text.Json.Serialization.JsonPropertyName("count")]
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
        [System.Text.Json.Serialization.JsonPropertyName("me")]
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

        [System.Text.Json.Serialization.JsonPropertyName("emoji")]
        public Emoji Emoji { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class Emoji
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        
        // Animated emojis support
        [System.Text.Json.Serialization.JsonPropertyName("animated")]
        public bool Animated { get; set; }

        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(Id))
                {
                    // Standard unicode emoji. 
                    // Discord doesn't provide a URL for unicode emojis in the API directly in the same way, 
                    // usually the client renders the unicode char.
                    // However, we might want to use Twemoji CDN if we want consistent look.
                    // For now, let's assume if it has no ID, we just display the Name (which is the unicode char).
                    return null;
                }
                string format = Animated ? "gif" : "png";
                return $"https://cdn.discordapp.com/emojis/{Id}.{format}";
            }
        }

        public bool IsCustom => !string.IsNullOrEmpty(Id);
    }

    public class Attachment
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("filename")]
        public string Filename { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("width")]
        public int? Width { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("height")]
        public int? Height { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("content_type")]
        public string ContentType { get; set; }
        
        public bool IsImage => ContentType != null && ContentType.StartsWith("image/");
    }

    public class Embed
    {
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("color")]
        public int? Color { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("image")]
        public EmbedImage Image { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("thumbnail")]
        public EmbedImage Thumbnail { get; set; }
        
        // Helper to convert int Color to Hex
        public string ColorHex 
        {
            get
            {
                if (Color.HasValue)
                {
                    var bytes = BitConverter.GetBytes(Color.Value);
                    // Standard int color is typically ARGB or RGB? Discord uses integer representation of RGB.
                    return string.Format("#{0:X6}", Color.Value & 0xFFFFFF);
                }
                return "#202225"; // Default embed border or background
            }
        }
    }

    public class EmbedImage
    {
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("width")]
        public int? Width { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("height")]
        public int? Height { get; set; }
    }

    public class Relationship : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id { get; set; }
        public int Type { get; set; } // 1 = Friend, 2 = Blocked, 3 = Incoming, 4 = Outgoing
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
        
        public Microsoft.UI.Xaml.Visibility HasActivityPrimaryImage => (PrimaryActivity != null && !string.IsNullOrEmpty(PrimaryActivity.PrimaryImageUrl)) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        public Microsoft.UI.Xaml.Visibility HasActivitySecondaryImage => (PrimaryActivity != null && !string.IsNullOrEmpty(PrimaryActivity.SecondaryImageUrl)) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;


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

        public Microsoft.UI.Xaml.Visibility ElapsedTimeVisibility => !string.IsNullOrEmpty(ElapsedTime) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
        
        public Microsoft.UI.Xaml.Visibility ActivityVisibility => HasActivity ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public Microsoft.UI.Xaml.Visibility RichPresenceCardVisibility => 
            (PrimaryActivity != null && PrimaryActivity.IsRichPresence && PrimaryActivity.Type != 4) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public class SearchResultItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string IconUrl { get; set; }
        public string Glyph { get; set; } // For font icons
        public string Type { get; set; } // "Channel", "User", "Server"
        public object OriginalObject { get; set; } // The Channel, User, or Server object

        public bool ShowImage => !string.IsNullOrEmpty(IconUrl);
        public bool ShowGlyph => !string.IsNullOrEmpty(Glyph);
    }

    // --- Gateway Models ---

    public class GatewayPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("op")]
        public int OpCode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("d")]
        public object Data { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("t")]
        public string EventName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("s")]
        public int? SequenceNumber { get; set; }
    }

    public class GatewayHello
    {
        [System.Text.Json.Serialization.JsonPropertyName("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }

    public class IdentifyProperties
    {
        [System.Text.Json.Serialization.JsonPropertyName("os")]
        public string Os { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("browser")]
        public string Browser { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("device")]
        public string Device { get; set; }
    }

    public class IdentifyPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("properties")]
        public IdentifyProperties Properties { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("intents")]
        public int Intents { get; set; }
    }

    public class Application
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("icon")]
        public string Icon { get; set; }
    }

    public class Activity
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public int Type { get; set; } // 0=Game, 1=Streaming, 2=Listening, 3=Watching, 4=Custom, 5=Competing

        [System.Text.Json.Serialization.JsonPropertyName("state")]
        public string State { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("details")]
        public string Details { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public ActivityAssets Assets { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("timestamps")]
        public ActivityTimestamps Timestamps { get; set; }

        // Mapped from external fetch
        public string ResolvedApplicationIconUrl { get; set; }

        // --- Helpers for Asset Resolution ---
        
        public string HeaderIconUrl
        {
            get
            {
                // Logic: 
                // 1. If has SmallImage, use it
                // 2. Else if has LargeImage, use it
                // 3. If we resolved an Application Icon (e.g. Roblox), use it!
                // 4. Fallback to generic icon based on Type
                
                string url = GetImageUrl(Assets?.SmallImage);
                if (url == null) url = GetImageUrl(Assets?.LargeImage);
                
                if (url != null) return url;
                
                // Use resolved app icon if we found one
                if (!string.IsNullOrEmpty(ResolvedApplicationIconUrl))
                    return ResolvedApplicationIconUrl;

                // Fallbacks (Local Assets)
                switch(Type)
                {
                    case 0: return "ms-appx:///Assets/generic_game_icon.png";
                    case 1: return "ms-appx:///Assets/generic_music_icon.png"; // Streaming
                    case 2: return "ms-appx:///Assets/generic_music_icon.png"; // Listening
                    default: return "ms-appx:///Assets/generic_game_icon.png";
                }
            }
        }

        public string PrimaryImageUrl 
        {
            get 
            {
                return GetImageUrl(Assets?.LargeImage);
                // Removed fallback to HeaderIconUrl to prevent showing generic icon in large card
            }
        } 
        
        public string SecondaryImageUrl => GetImageUrl(Assets?.SmallImage);
        
        public bool HasPrimaryImage => !string.IsNullOrEmpty(PrimaryImageUrl);
        public bool HasSecondaryImage => !string.IsNullOrEmpty(SecondaryImageUrl);
        
        // Only show the big rich presence card if we actually have rich data (Images, Details, or specific State)
        public bool IsRichPresence => (Assets != null && (HasPrimaryImage || HasSecondaryImage)) || !string.IsNullOrEmpty(Details) || !string.IsNullOrEmpty(State);

        private string GetImageUrl(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;

            // 1. Spotify
            if (assetId.StartsWith("spotify:"))
            {
                return $"https://i.scdn.co/image/{assetId.Substring("spotify:".Length)}";
            }

            // 2. External
            if (assetId.StartsWith("mp:external/"))
            {
                // The format is usually mp:external/{path_base64}/{filename} -> which maps to MEDIA_PROXY.
                // Simplified: https://media.discordapp.net/external/... 
                // However, without complex parsing, we can try to reconstruct if it's a known pattern, 
                // OR we accept that we might need the full url. 
                // Actually, often it is simpler: just replace mp: with https://media.discordapp.net/
                return assetId.Replace("mp:", "https://media.discordapp.net/");
            }

            // 3. Standard Discord CDN Asset
            if (!string.IsNullOrEmpty(ApplicationId))
            {
                return $"https://cdn.discordapp.com/app-assets/{ApplicationId}/{assetId}.png";
            }

            return null;
        }
    }

    public class ActivityTimestamps
    {
        [System.Text.Json.Serialization.JsonPropertyName("start")]
        public long? Start { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("end")]
        public long? End { get; set; }
    }

    public class ActivityAssets
    {
        [System.Text.Json.Serialization.JsonPropertyName("large_image")]
        public string LargeImage { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("large_text")]
        public string LargeText { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("small_image")]
        public string SmallImage { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("small_text")]
        public string SmallText { get; set; }
    }

    public class PresenceUpdate
    {
        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public User User { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } // "online", "dnd", "idle", "invisible", "offline"

        [System.Text.Json.Serialization.JsonPropertyName("activities")]
        public List<Activity> Activities { get; set; }
    }

    public class VoiceState
    {
        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("member")]
        public GuildMember Member { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("deaf")]
        public bool Deaf { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("mute")]
        public bool Mute { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("self_deaf")]
        public bool SelfDeaf { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("self_mute")]
        public bool SelfMute { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("self_video")]
        public bool SelfVideo { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("suppress")]
        public bool Suppress { get; set; }
    }

    public class ReadyPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("v")]
        public int Version { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public User User { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("presences")]
        public List<PresenceUpdate> Presences { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("guilds")]
        public List<Server> Guilds { get; set; }
    }

    public class MessageDeletedPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }
    }

    public class TypingStartPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("member")]
        public GuildMember Member { get; set; }
    }

    public class MessageReactionUpdatePayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("emoji")]
        public Emoji Emoji { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message_id")]
        public string MessageId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }
    }

    public class MessageReference
    {
        [System.Text.Json.Serialization.JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fail_if_not_exists")]
        public bool? FailIfNotExists { get; set; }
    }
}
