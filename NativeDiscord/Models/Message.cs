using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Message
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("edited_timestamp")]
        public DateTimeOffset? EditedTimestamp { get; set; }

        [JsonPropertyName("author")]
        public User Author { get; set; }

        [JsonPropertyName("attachments")]
        public List<Attachment> Attachments { get; set; }

        [JsonPropertyName("embeds")]
        public List<Embed> Embeds { get; set; }

        [JsonPropertyName("reactions")]
        public List<Reaction> Reactions { get; set; }

        [JsonPropertyName("message_reference")]
        public MessageReference MessageReference { get; set; }

        [JsonPropertyName("referenced_message")]
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

    public class Attachment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        public bool IsImage => ContentType != null && ContentType.StartsWith("image/");
    }

    public class MessageReference
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("fail_if_not_exists")]
        public bool? FailIfNotExists { get; set; }
    }

    public class MessageDeletedPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }
    }
}
