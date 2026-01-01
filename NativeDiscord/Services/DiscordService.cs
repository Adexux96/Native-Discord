using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NativeDiscord.Services
{
    public class DiscordService
    {
        private string _token;

        public DiscordHttpService Http { get; private set; }
        public DiscordGatewayService Gateway { get; private set; }
        public NativeDiscord.Models.User CurrentUser { get; private set; }
        public System.Collections.Generic.List<NativeDiscord.Models.Server> Guilds { get; private set; }
        public System.Collections.Generic.List<NativeDiscord.Models.Relationship> Relationships { get; private set; }

        public Microsoft.UI.Dispatching.DispatcherQueue Dispatcher { get; set; }

        private Dictionary<string, NativeDiscord.Models.User> _userCache = new Dictionary<string, NativeDiscord.Models.User>();
        private HashSet<string> _pendingUserRequests = new HashSet<string>();
        public event EventHandler<string> UserResolved;

        private Dictionary<string, NativeDiscord.Models.Channel> _channelCache = new Dictionary<string, NativeDiscord.Models.Channel>();
        private HashSet<string> _pendingChannelRequests = new HashSet<string>();
        public event EventHandler<string> ChannelResolved;

        public event EventHandler<string> NavigationRequested;
        public void RequestNavigation(string channelId) => NavigationRequested?.Invoke(this, channelId);

        public DiscordService()
        {
            // Initialize Http Service
            Http = new DiscordHttpService();
            Gateway = new DiscordGatewayService();
            
            Gateway.OnPresenceUpdate += Gateway_OnPresenceUpdate;
            Gateway.OnMessageCreate += Gateway_OnMessageCreate;
            Gateway.OnMessageUpdate += Gateway_OnMessageUpdate;
            Gateway.OnMessageDelete += Gateway_OnMessageDelete;
            Gateway.OnVoiceStateUpdate += Gateway_OnVoiceStateUpdate;
            Gateway.OnTypingStart += Gateway_OnTypingStart;
            Gateway.OnMessageReactionAdd += Gateway_OnMessageReactionAdd;
            Gateway.OnMessageReactionRemove += Gateway_OnMessageReactionRemove;
            Gateway.OnReady += Gateway_OnReady;
        }

        private void Gateway_OnReady(object sender, NativeDiscord.Models.ReadyPayload e)
        {
             // Dispatch to UI thread to safely update models bound to UI
             if (Dispatcher != null)
             {
                 Dispatcher.TryEnqueue(() => 
                 {
                     ProcessReadyPayload(e);
                 });
             }
             else
             {
                 ProcessReadyPayload(e);
             }
        }

        private Dictionary<string, string> _appIconCache = new Dictionary<string, string>();

        private void ProcessReadyPayload(NativeDiscord.Models.ReadyPayload e)
        {
            if (e.Presences != null)
            {
                if (Relationships != null)
                {
                    ApplyPresences(e.Presences);
                }
                else
                {
                    _pendingPresences = e.Presences;
                }
            }
        }

        private List<NativeDiscord.Models.PresenceUpdate> _pendingPresences;

        private void ApplyPresences(List<NativeDiscord.Models.PresenceUpdate> presences)
        {
             foreach (var p in presences)
             {
                 if (p.User == null) continue;
                 
                 var rel = Relationships.Find(r => r.Id == p.User.Id || (r.User != null && r.User.Id == p.User.Id));
                 if (rel != null)
                 {
                     rel.Status = p.Status;
                     rel.Activities = p.Activities;
                     CheckResolveIcons(rel);
                 }
             }
             System.Diagnostics.Debug.WriteLine($"Processed {presences.Count} presences.");

             if (presences.Count > 0)
             {
                 PresenceUpdated?.Invoke(this, presences[0]); 
             }
        }

        private void CheckResolveIcons(NativeDiscord.Models.Relationship rel)
        {
            if (rel.Activities != null)
            {
                foreach (var act in rel.Activities)
                {
                    // If it's falling back to generic icon (local asset) but has an App ID, try to resolve real icon
                    if (!string.IsNullOrEmpty(act.ApplicationId) && 
                        (string.IsNullOrEmpty(act.HeaderIconUrl) || act.HeaderIconUrl.StartsWith("ms-appx")))
                    {
                        ResolveActivityIcon(rel, act);
                    }
                }
            }
        }

        private async void ResolveActivityIcon(NativeDiscord.Models.Relationship rel, NativeDiscord.Models.Activity act)
        {
            if (_appIconCache.ContainsKey(act.ApplicationId))
            {
                act.ResolvedApplicationIconUrl = _appIconCache[act.ApplicationId];
                TriggerActivityUpdate(rel);
                return;
            }

            try
            {
                var app = await Http.GetApplicationRpcInfoAsync(act.ApplicationId);
                if (app != null && !string.IsNullOrEmpty(app.Icon))
                {
                    string url = $"https://cdn.discordapp.com/app-icons/{app.Id}/{app.Icon}.png";
                    _appIconCache[act.ApplicationId] = url;
                    
                    act.ResolvedApplicationIconUrl = url;
                    TriggerActivityUpdate(rel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to resolve app icon for {act.ApplicationId}: {ex.Message}");
            }
        }

        private void TriggerActivityUpdate(NativeDiscord.Models.Relationship rel)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() =>
                {
                    // Force property changed notification to refresh UI
                    var temp = rel.Activities;
                    rel.Activities = null;
                    rel.Activities = temp;
                });
            }
        }

        public event EventHandler<NativeDiscord.Models.PresenceUpdate> PresenceUpdated;

        private void Gateway_OnPresenceUpdate(object sender, NativeDiscord.Models.PresenceUpdate e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() => 
                {
                    ProcessPresenceUpdate(e);
                });
            }
            else
            {
                ProcessPresenceUpdate(e);
            }
        }

        private void ProcessPresenceUpdate(NativeDiscord.Models.PresenceUpdate e)
        {
            if (Relationships != null && e.User != null)
            {
                var rel = Relationships.Find(r => r.Id == e.User.Id || (r.User != null && r.User.Id == e.User.Id));
                if (rel != null)
                {
                    rel.Status = e.Status;
                    rel.Activities = e.Activities;
                    CheckResolveIcons(rel);
                    System.Diagnostics.Debug.WriteLine($"Updated user {rel.User.Username} to {rel.Status} ({rel.ActivityText})");
                }
            }
            
            PresenceUpdated?.Invoke(this, e);
        }

        public string GetUserStatus(string userId)
        {
            if (Relationships != null)
            {
                var rel = Relationships.Find(r => r.Id == userId || (r.User != null && r.User.Id == userId));
                if (rel != null) return rel.Status;
            }
            return "offline";
        }
        
        // Backward compatibility
        public bool IsUserOnline(string userId)
        {
             var status = GetUserStatus(userId);
             return status != "offline" && status != "invisible";
        }

        public event EventHandler<NativeDiscord.Models.Message> MessageReceived;
        public event EventHandler<NativeDiscord.Models.Message> MessageUpdated;
        public event EventHandler<NativeDiscord.Models.MessageDeletedPayload> MessageDeleted;

        public NativeDiscord.Models.User GetCachedUser(string id)
        {
            if (_userCache.ContainsKey(id)) return _userCache[id];
            
            // Fallback to Relationships (Friends)
            if (Relationships != null)
            {
                var rel = Relationships.Find(r => r.User != null && r.User.Id == id);
                if (rel != null)
                {
                    _userCache[id] = rel.User;
                    return rel.User;
                }
            }

            // Fallback to CurrentUser
            if (CurrentUser != null && CurrentUser.Id == id)
            {
                _userCache[id] = CurrentUser;
                return CurrentUser;
            }

            return null;
        }

        public async void RequestUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_userCache.ContainsKey(id)) return; // Already cached
            if (_pendingUserRequests.Contains(id)) return; // Already requested
            
            _pendingUserRequests.Add(id);

            try
            {
                var user = await Http.GetUserAsync(id);
                if (user != null)
                {
                    UpdateUserCache(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch user {id}: {ex.Message}");
            }
            finally
            {
                _pendingUserRequests.Remove(id);
            }
        }

        public void UpdateUserCache(NativeDiscord.Models.User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id)) return;
            
            _userCache[user.Id] = user;
            
            Dispatcher?.TryEnqueue(() => UserResolved?.Invoke(this, user.Id));
        }

        public NativeDiscord.Models.Channel GetCachedChannel(string id)
        {
            if (_channelCache.ContainsKey(id)) return _channelCache[id];
            
            // Fallback to Guilds loop (if we loaded channels there? currently we don't seem to store them centrally in DiscordService except via this cache)
            // But we can check if we have them.
            return null;
        }

        public async void RequestChannel(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_channelCache.ContainsKey(id)) return;
            if (_pendingChannelRequests.Contains(id)) return;

            _pendingChannelRequests.Add(id);
            try
            {
                var channel = await Http.GetChannelAsync(id);
                if (channel != null)
                {
                    UpdateChannelCache(channel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch channel {id}: {ex.Message}");
            }
            finally
            {
                _pendingChannelRequests.Remove(id);
            }
        }

        public void UpdateChannelCache(NativeDiscord.Models.Channel channel)
        {
            if (channel == null || string.IsNullOrEmpty(channel.Id)) return;
            _channelCache[channel.Id] = channel;
            Dispatcher?.TryEnqueue(() => ChannelResolved?.Invoke(this, channel.Id));
        }



        private void Gateway_OnMessageCreate(object sender, NativeDiscord.Models.Message e)
        {
            if (e.Author != null) UpdateUserCache(e.Author);
            MessageReceived?.Invoke(this, e);
        }

        private void Gateway_OnMessageUpdate(object sender, NativeDiscord.Models.Message e)
        {
            MessageUpdated?.Invoke(this, e);
        }

        private void Gateway_OnMessageDelete(object sender, NativeDiscord.Models.MessageDeletedPayload e)
        {
            MessageDeleted?.Invoke(this, e);
        }

        // Voice State Management
        private Dictionary<string, List<NativeDiscord.Models.VoiceState>> _guildVoiceStates = new Dictionary<string, List<NativeDiscord.Models.VoiceState>>();

        public event EventHandler<NativeDiscord.Models.VoiceState> VoiceStateUpdated;

        private void Gateway_OnVoiceStateUpdate(object sender, NativeDiscord.Models.VoiceState e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() => ProcessVoiceStateUpdate(e));
            }
            else
            {
                ProcessVoiceStateUpdate(e);
            }
        }

        private void Gateway_OnTypingStart(object sender, NativeDiscord.Models.TypingStartPayload e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() => UserTyping?.Invoke(this, e));
            }
            else
            {
                UserTyping?.Invoke(this, e);
            }
        }

        public event EventHandler<NativeDiscord.Models.TypingStartPayload> UserTyping;
        public event EventHandler<NativeDiscord.Models.MessageReactionUpdatePayload> MessageReactionAdded;
        public event EventHandler<NativeDiscord.Models.MessageReactionUpdatePayload> MessageReactionRemoved;

        private void Gateway_OnMessageReactionAdd(object sender, NativeDiscord.Models.MessageReactionUpdatePayload e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() => MessageReactionAdded?.Invoke(this, e));
            }
            else
            {
                MessageReactionAdded?.Invoke(this, e);
            }
        }

        private void Gateway_OnMessageReactionRemove(object sender, NativeDiscord.Models.MessageReactionUpdatePayload e)
        {
            if (Dispatcher != null)
            {
                Dispatcher.TryEnqueue(() => MessageReactionRemoved?.Invoke(this, e));
            }
            else
            {
                MessageReactionRemoved?.Invoke(this, e);
            }
        }

        private void ProcessVoiceStateUpdate(NativeDiscord.Models.VoiceState e)
        {
            if (string.IsNullOrEmpty(e.GuildId)) return;

            if (!_guildVoiceStates.ContainsKey(e.GuildId))
            {
                _guildVoiceStates[e.GuildId] = new List<NativeDiscord.Models.VoiceState>();
            }

            var states = _guildVoiceStates[e.GuildId];
            var existing = states.Find(s => s.UserId == e.UserId);

            if (existing != null)
            {
                states.Remove(existing);
            }

            // If ChannelId is NOT null, add/update. If null, they left, so we just removed them above.
            if (!string.IsNullOrEmpty(e.ChannelId))
            {
                // Ensure we have minimal Member data if missing (sometimes only UserId comes if update is self_mute toggle?)
                // Actually VOICE_STATE_UPDATE usually has member.
                states.Add(e);
            }

            VoiceStateUpdated?.Invoke(this, e);
        }

        public List<NativeDiscord.Models.VoiceState> GetVoiceStates(string guildId)
        {
            if (_guildVoiceStates.ContainsKey(guildId))
            {
                return _guildVoiceStates[guildId];
            }
            return new List<NativeDiscord.Models.VoiceState>();
        }

        public async Task LoginAsync(string token)
        {
            _token = token;
            
            // Initialize HTTP Service with the user token
            Http.SetToken(token);
            
            // Initialize Gateway
            _ = Gateway.ConnectAsync(token);

            await Task.CompletedTask;
        }

        public async Task InitializeDataAsync()
        {
            CurrentUser = await Http.GetCurrentUserAsync();
            Guilds = await Http.GetGuildsAsync();
            Relationships = await Http.GetRelationshipsAsync();

            // Seed Cache
            if (CurrentUser != null) _userCache[CurrentUser.Id] = CurrentUser;
            if (Relationships != null)
            {
                foreach (var rel in Relationships)
                {
                     if (rel.User != null) _userCache[rel.User.Id] = rel.User;
                }
            }
            
            if (_pendingPresences != null && Relationships != null)
            {
                ApplyPresences(_pendingPresences);
                _pendingPresences = null;
            }

            await Http.LoadRecentsAsync();
        }
    }
}
