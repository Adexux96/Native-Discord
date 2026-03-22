using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class PresenceUpdate
    {
        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } // "online", "dnd", "idle", "invisible", "offline"

        [JsonPropertyName("activities")]
        public List<Activity> Activities { get; set; }
    }
}
