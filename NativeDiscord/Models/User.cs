using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }

        [JsonPropertyName("global_name")]
        public string GlobalName { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } // Raw hash

        [JsonPropertyName("status")]
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

    public class GuildMember
    {
        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; }
    }
}
