using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class GatewayPayload
    {
        [JsonPropertyName("op")]
        public int OpCode { get; set; }

        [JsonPropertyName("d")]
        public object Data { get; set; }

        [JsonPropertyName("t")]
        public string EventName { get; set; }

        [JsonPropertyName("s")]
        public int? SequenceNumber { get; set; }
    }

    public class GatewayHello
    {
        [JsonPropertyName("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }

    public class IdentifyProperties
    {
        [JsonPropertyName("os")]
        public string Os { get; set; }

        [JsonPropertyName("browser")]
        public string Browser { get; set; }

        [JsonPropertyName("device")]
        public string Device { get; set; }
    }

    public class IdentifyPayload
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("properties")]
        public IdentifyProperties Properties { get; set; }

        [JsonPropertyName("intents")]
        public int Intents { get; set; }
    }

    public class ReadyPayload
    {
        [JsonPropertyName("v")]
        public int Version { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("presences")]
        public List<PresenceUpdate> Presences { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; }

        [JsonPropertyName("guilds")]
        public List<Server> Guilds { get; set; }
    }

    public class TypingStartPayload
    {
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("member")]
        public GuildMember Member { get; set; }
    }

    public class MessageReactionUpdatePayload
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("emoji")]
        public Emoji Emoji { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }
    }
}
