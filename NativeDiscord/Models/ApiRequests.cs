using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class MessageRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("message_reference")]
        public MessageReference MessageReference { get; set; }
    }

    public class EditMessageRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class FriendRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }
    }
}
