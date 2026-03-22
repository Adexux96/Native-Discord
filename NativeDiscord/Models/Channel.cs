using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Channel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; } // 0 = Text, 2 = Voice, 4 = Category

        [JsonPropertyName("parent_id")]
        public string ParentId { get; set; } // Category ID

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("permission_overwrites")]
        public List<PermissionOverwrite> PermissionOverwrites { get; set; }

        [JsonPropertyName("recipients")]
        public List<User> Recipients { get; set; }

        [JsonPropertyName("user_limit")]
        public int UserLimit { get; set; }
    }

    public class PermissionOverwrite
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } // Role or User ID

        [JsonPropertyName("type")]
        public int Type { get; set; } // 0 = Role, 1 = Member

        [JsonPropertyName("allow")]
        public string Allow { get; set; } // Bitmask

        [JsonPropertyName("deny")]
        public string Deny { get; set; } // Bitmask
    }
}
