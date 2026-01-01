using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NativeDiscord.Models;

namespace NativeDiscord.Services
{
    public class MockDataService
    {
        public Task<List<Server>> GetServersAsync()
        {
            var servers = new List<Server>
            {
                new Server { Id = "1", Name = "Native Discord Devs", Icon = "icon_hash_1" },
                new Server { Id = "2", Name = "C# Community", Icon = "icon_hash_2" },
                new Server { Id = "3", Name = "Gaming Lounge", Icon = "icon_hash_3" }
            };
            return Task.FromResult(servers);
        }

        public Task<List<Channel>> GetChannelsAsync(string serverId)
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "101", Name = "general", Type = 0 },
                new Channel { Id = "102", Name = "announcements", Type = 0 },
                new Channel { Id = "103", Name = "dev-talk", Type = 0 },
                new Channel { Id = "104", Name = "Voice Lounge", Type = 2 }
            };
            return Task.FromResult(channels);
        }

        public Task<List<Message>> GetMessagesAsync(string channelId)
        {
            var messages = new List<Message>();
            for (int i = 0; i < 20; i++)
            {
                messages.Add(new Message
                {
                    Id = i.ToString(),
                    Content = $"This is message #{i} in channel {channelId}. It represents some chat content.",
                    Timestamp = DateTimeOffset.Now.AddMinutes(-i * 5),
                    Author = new User { Id = "u1", Username = "Adexux" }
                });
            }
            return Task.FromResult(messages);
        }
    }
}
