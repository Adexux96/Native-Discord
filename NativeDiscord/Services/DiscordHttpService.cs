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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loaded = JsonSerializer.Deserialize<List<Channel>>(json, options);
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
                
                var json = JsonSerializer.Serialize(RecentChannels);
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

        public async Task<List<Relationship>> GetRelationshipsAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me/relationships");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Relationship>>(json, options);
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
             
             object payload;
             if (!string.IsNullOrEmpty(discriminator))
                payload = new { username = username, discriminator = discriminator };
             else
                payload = new { username = username };

             var jsonPayload = JsonSerializer.Serialize(payload);
             var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

             var response = await _httpClient.PostAsync(BaseUrl + "/users/@me/relationships", httpContent);
             response.EnsureSuccessStatusCode();
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<User>(json, options);
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/users/{userId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<User>(json, options);
        }

        public async Task<List<Server>> GetGuildsAsync()
        {
            var response = await _httpClient.GetAsync(BaseUrl + "/users/@me/guilds");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Server>>(json, options);
        }

        public async Task<List<Channel>> GetChannelsAsync(string guildId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/channels");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var channels = JsonSerializer.Deserialize<List<Channel>>(json, options);
            
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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Channel>(json, options);
        }

        public async Task<GuildMember> GetGuildMemberAsync(string guildId, string userId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/members/{userId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GuildMember>(json, options);
        }

        public async Task<List<Role>> GetRolesAsync(string guildId)
        {
            var response = await _httpClient.GetAsync(BaseUrl + $"/guilds/{guildId}/roles");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Role>>(json, options);
        }

        public async Task<List<Message>> GetMessagesAsync(string channelId)
        {
            // Fetch last 50 messages
            var response = await _httpClient.GetAsync(BaseUrl + $"/channels/{channelId}/messages?limit=50");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Message>>(json, options);
        }

        public async Task SendMessageAsync(string channelId, string content, MessageReference messageReference = null)
        {
            object payload;
            if (messageReference != null)
            {
                payload = new 
                { 
                    content = content,
                    message_reference = messageReference
                };
            }
            else
            {
                payload = new { content = content };
            }

            var jsonPayload = JsonSerializer.Serialize(payload);
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
            var payload = new { content = content };
            var jsonPayload = JsonSerializer.Serialize(payload);
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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Channel>>(json, options);
        }


        public async Task<Application> GetApplicationRpcInfoAsync(string applicationId)
        {
            // Fetch public RPC info for the application to get the icon.
            // Note: This endpoint is used by the client to populate game info.
            var response = await _httpClient.GetAsync(BaseUrl + $"/applications/{applicationId}/rpc");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Application>(json, options);
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
