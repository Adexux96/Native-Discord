using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NativeDiscord.Models;
using Windows.Storage;

namespace NativeDiscord.Services
{
    public class DiscordHttpService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://discord.com/api/v9";


        public DiscordHttpService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            
            // Spoof a standard browser/client user agent to avoid immediate blocks (basic level)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        private const string RecentsFileName = "recent_channels.json";

        public List<Channel> RecentChannels { get; set; } = new List<Channel>();

        public void AddToRecentChannels(Channel channel)
        {
            if (channel == null) return;

            // Remove if already exists (to move to top)
            var existing = RecentChannels.Find(c => c.Id == channel.Id);
            if (existing != null)
            {
                RecentChannels.Remove(existing);
            }

            // Insert at top
            RecentChannels.Insert(0, channel);

            // Limit to 20
            if (RecentChannels.Count > 20)
            {
                RecentChannels.RemoveAt(RecentChannels.Count - 1);
            }

            _ = SaveRecentsAsync();
        }

        public async Task LoadRecentsAsync()
        {
            try
            {
                // Use standard IO for Unpackaged app
                var folderPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NativeDiscord");
                System.IO.Directory.CreateDirectory(folderPath);
                
                var filePath = System.IO.Path.Combine(folderPath, RecentsFileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    var loaded = JsonSerializer.Deserialize<List<Channel>>(json, DefaultOptions);
                    if (loaded != null)
                    {
                        RecentChannels = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading recents: {ex}");
            }
        }

        public async Task SaveRecentsAsync()
        {
            try
            {
                var folderPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NativeDiscord");
                System.IO.Directory.CreateDirectory(folderPath);

                var filePath = System.IO.Path.Combine(folderPath, RecentsFileName);
                
                var json = JsonSerializer.Serialize(RecentChannels, DefaultOptions);
                await System.IO.File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving recents: {ex}");
            }
        }

        public void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token); // User tokens don't use "Bot" prefix
        }

        private static JsonSerializerOptions DefaultOptions => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = Helpers.DiscordJsonContext.Default
        };

        public async Task<List<Relationship>> GetRelationshipsAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me/relationships");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Relationship>>(json, DefaultOptions);
        }

        public async Task<List<User>> GetFriendsAsync()
        {
           var relationships = await GetRelationshipsAsync();
           var friends = new List<User>();
           if (relationships != null)
           {
               foreach (var rel in relationships)
               {
                   if (rel.Type == 1 && rel.User != null) // 1 is Friend
                   {
                       friends.Add(rel.User);
                   }
               }
           }
           return friends;
        }

        public async Task SendFriendRequestAsync(string username, string discriminator = null)
        {
             // POST /users/@me/relationships
             // Body: { "username": "name", "discriminator": "1234" }
             
             string jsonPayload;
             if (!string.IsNullOrEmpty(discriminator))
                jsonPayload = JsonSerializer.Serialize(new { username = username, discriminator = discriminator }, DefaultOptions);
             else
                jsonPayload = JsonSerializer.Serialize(new { username = username }, DefaultOptions);

             var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

             var response = await _httpClient.PostAsync(BaseUrl + "/users/@me/relationships", httpContent);
             response.EnsureSuccessStatusCode();
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(json, DefaultOptions);
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/users/{userId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(json, DefaultOptions);
        }

        public async Task<List<Server>> GetGuildsAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me/guilds");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Server>>(json, DefaultOptions);
        }

        public async Task<List<Channel>> GetChannelsAsync(string guildId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/channels");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var channels = JsonSerializer.Deserialize<List<Channel>>(json, DefaultOptions);
            
            // Ensure GuildId is set (API usually sends it, but let's be safe)
            if (channels != null)
            {
                foreach (var c in channels)
                {
                    if (string.IsNullOrEmpty(c.GuildId)) c.GuildId = guildId;
                }
            }
            return channels;
        }

        public async Task<Channel> GetChannelAsync(string channelId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/channels/{channelId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Channel>(json, DefaultOptions);
        }

        public async Task<GuildMember> GetGuildMemberAsync(string guildId, string userId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/members/{userId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GuildMember>(json, DefaultOptions);
        }

        public async Task<List<Role>> GetRolesAsync(string guildId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/roles");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Role>>(json, DefaultOptions);
        }

        public async Task<List<Message>> GetMessagesAsync(string channelId, string before = null)
        {
            // Fetch last 50 messages
            string url = BaseUrl + $"/channels/{channelId}/messages?limit=50";
            if (!string.IsNullOrEmpty(before))
            {
                url += $"&before={before}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Message>>(json, DefaultOptions);
        }

        public async Task SendMessageAsync(string channelId, string content, MessageReference messageReference = null)
        {
            string jsonPayload;
            if (messageReference != null)
            {
                jsonPayload = JsonSerializer.Serialize(new
                { 
                    content = content,
                    message_reference = messageReference
                }, DefaultOptions);
            }
            else
            {
                jsonPayload = JsonSerializer.Serialize(new { content = content }, DefaultOptions);
            }

            var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(BaseUrl + $"/channels/{channelId}/messages", httpContent);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteMessageAsync(string channelId, string messageId)
        {
            var response = await _httpClient.DeleteAsync(BaseUrl + $"/channels/{channelId}/messages/{messageId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task EditMessageAsync(string channelId, string messageId, string content)
        {
            var jsonPayload = JsonSerializer.Serialize(new { content = content }, DefaultOptions);
            var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync(BaseUrl + $"/channels/{channelId}/messages/{messageId}", httpContent);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Channel>> GetPrivateChannelsAsync()
        {
            // GET /users/@me/channels returns list of DM channels
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me/channels");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Channel>>(json, DefaultOptions);
        }


        public async Task<Application> GetApplicationRpcInfoAsync(string applicationId)
        {
            // Fetch public RPC info for the application to get the icon.
            // Note: This endpoint is used by the client to populate game info.
            var response = await _httpClient.GetAsync(BaseUrl + $"/applications/{applicationId}/rpc");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Application>(json, DefaultOptions);
        }
        public async Task AddReactionAsync(string channelId, string messageId, Emoji emoji)
        {
            // Format: name:id for custom, name (unicode) for standard
            string emojiCode = string.IsNullOrEmpty(emoji.Id) ? emoji.Name : $"{emoji.Name}:{emoji.Id}";
            // URL Encode the emoji code (important for unicode)
            emojiCode = System.Net.WebUtility.UrlEncode(emojiCode);

            var response = await _httpClient.PutAsync(BaseUrl + $"/channels/{channelId}/messages/{messageId}/reactions/{emojiCode}/@me", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteReactionAsync(string channelId, string messageId, Emoji emoji)
        {
            string emojiCode = string.IsNullOrEmpty(emoji.Id) ? emoji.Name : $"{emoji.Name}:{emoji.Id}";
            emojiCode = System.Net.WebUtility.UrlEncode(emojiCode);

            var response = await _httpClient.DeleteAsync(BaseUrl + $"/channels/{channelId}/messages/{messageId}/reactions/{emojiCode}/@me");
            response.EnsureSuccessStatusCode();
        }
    }
}
