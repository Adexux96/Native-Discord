using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class VoiceState
    {
        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("member")]
        public GuildMember Member { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("deaf")]
        public bool Deaf { get; set; }

        [JsonPropertyName("mute")]
        public bool Mute { get; set; }

        [JsonPropertyName("self_deaf")]
        public bool SelfDeaf { get; set; }

        [JsonPropertyName("self_mute")]
        public bool SelfMute { get; set; }

        [JsonPropertyName("self_video")]
        public bool SelfVideo { get; set; }

        [JsonPropertyName("suppress")]
        public bool Suppress { get; set; }
    }
}
