using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Server
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
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

        [JsonPropertyName("voice_states")]
        public List<VoiceState> VoiceStates { get; set; }
    }

    public class Role
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; } // Bitmask

        [JsonPropertyName("position")]
        public int Position { get; set; }
    }
}
