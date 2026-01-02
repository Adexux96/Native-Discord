using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NativeDiscord.Models;
using NativeDiscord.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace NativeDiscord.Views
{
    public sealed partial class ServerPage : Page
    {
        private DiscordService _discordService;
        private Server _currentServer;
        private string _targetChannelId;
        public ObservableCollection<ChannelViewItem> ChannelItems { get; } = new ObservableCollection<ChannelViewItem>();
        
        // Helper to quickly find items by channel ID
        private System.Collections.Generic.Dictionary<string, ChannelViewItem> _channelMap = new System.Collections.Generic.Dictionary<string, ChannelViewItem>();

        public ServerPage()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ServerContext context)
            {
                _discordService = context.Service;
                _currentServer = context.Server;
                _targetChannelId = context.TargetChannelId;
                
                ServerTitle.Text = _currentServer.Name;
                
                // Populate User Footer
                if (_discordService.CurrentUser != null)
                {
                    CurrentUserDisplayName.Text = _discordService.CurrentUser.DisplayName;
                    CurrentUserUsername.Text = _discordService.CurrentUser.Username;
                    if (!string.IsNullOrEmpty(_discordService.CurrentUser.AvatarUrl))
                    {
                        CurrentUserAvatar.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(_discordService.CurrentUser.AvatarUrl));
                    }
                }

                await LoadChannelsAsync();
                
                _discordService.VoiceStateUpdated += DiscordService_VoiceStateUpdated;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (_discordService != null)
            {
                 _discordService.VoiceStateUpdated -= DiscordService_VoiceStateUpdated;
            }
        }

        private void DiscordService_VoiceStateUpdated(object sender, NativeDiscord.Models.VoiceState e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.GuildId != _currentServer.Id) return;

                // 1. Remove user from ANY channel they might be in
                foreach (var item in ChannelItems)
                {
                   var existingUser = item.VoiceUsers.FirstOrDefault(u => u.Id == e.UserId);
                   if (existingUser != null)
                   {
                       item.VoiceUsers.Remove(existingUser);
                   }
                }

                // 2. Add to new channel if applicable
                if (!string.IsNullOrEmpty(e.ChannelId) && _channelMap.ContainsKey(e.ChannelId))
                {
                    var channelItem = _channelMap[e.ChannelId];
                    var user = new VoiceUser 
                    { 
                        Id = e.UserId, 
                        Name = e.Member?.User?.DisplayName ?? e.UserId,
                        AvatarUrl = e.Member?.User?.AvatarUrl ?? "https://cdn.discordapp.com/embed/avatars/0.png",
                        IsMuted = e.Mute || e.SelfMute,
                        IsDeafened = e.Deaf || e.SelfDeaf
                    };
                    channelItem.VoiceUsers.Add(user);
                }
            });
        }

        private async System.Threading.Tasks.Task LoadChannelsAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                
                // 1. Fetch All Data in Parallel
                var channelsTask = _discordService.Http.GetChannelsAsync(_currentServer.Id);
                var rolesTask = _discordService.Http.GetRolesAsync(_currentServer.Id);
                var memberTask = _discordService.Http.GetGuildMemberAsync(_currentServer.Id, _discordService.CurrentUser.Id);

                await System.Threading.Tasks.Task.WhenAll(channelsTask, rolesTask, memberTask);

                var allChannels = channelsTask.Result;
                var roles = rolesTask.Result;
                var member = memberTask.Result;

                foreach (var channel in allChannels)
                {
                    _discordService.UpdateChannelCache(channel);
                }

                ChannelItems.Clear();
                _channelMap.Clear();

                // Load initial voice states
                var voiceStates = _discordService.GetVoiceStates(_currentServer.Id);

                // 2. Identify Categories & Viewable Channels
                var categories = allChannels.Where(c => c.Type == 4).OrderBy(c => c.Position).ToList();
                
                var viewableChannels = allChannels.Where(c => 
                {
                    if (c.Type != 0 && c.Type != 2) return false;
                    return HasPermission(c, member, roles, 1024); // VIEW_CHANNEL
                }).ToList();

                // 3. Build the List
                
                // A) No Category Channels first
                var noCategoryChannels = viewableChannels
                    .Where(c => string.IsNullOrEmpty(c.ParentId))
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Position)
                    .ToList();
                
                foreach (var channel in noCategoryChannels)
                {
                    bool canWrite = HasPermission(channel, member, roles, 2048);
                    var item = new ChannelViewItem { Channel = channel, IsCategory = false, CanWrite = canWrite };
                    PopulateVoiceUsers(item, voiceStates);
                    ChannelItems.Add(item);
                    _channelMap[channel.Id] = item;
                }

                // B) Categories and their children
                foreach (var category in categories)
                {
                    // Find children for this category
                    var children = viewableChannels
                        .Where(c => c.ParentId == category.Id)
                        .OrderBy(c => c.Type)
                        .ThenBy(c => c.Position)
                        .ToList();

                    if (children.Any()) 
                    {
                        ChannelItems.Add(new ChannelViewItem { Channel = category, IsCategory = true });

                        foreach (var child in children)
                        {
                            bool canWrite = HasPermission(child, member, roles, 2048);
                            var item = new ChannelViewItem { Channel = child, IsCategory = false, CanWrite = canWrite };
                            PopulateVoiceUsers(item, voiceStates);
                            ChannelItems.Add(item);
                            _channelMap[child.Id] = item;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading channels: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                
                // Handle Deep Linking
                if (!string.IsNullOrEmpty(_targetChannelId))
                {
                    if (_channelMap.ContainsKey(_targetChannelId))
                    {
                        var item = _channelMap[_targetChannelId];
                        ChannelList.SelectedItem = item;
                        ChannelList.ScrollIntoView(item);
                        _targetChannelId = null; // Clear after use
                    }
                }
            }
        }

        private void PopulateVoiceUsers(ChannelViewItem item, System.Collections.Generic.List<NativeDiscord.Models.VoiceState> states)
        {
            if (item.Channel.Type == 2) // Voice
            {
                var usersInChannel = states.Where(s => s.ChannelId == item.Channel.Id).ToList();
                foreach (var s in usersInChannel)
                {
                    item.VoiceUsers.Add(new VoiceUser 
                    { 
                        Id = s.UserId,
                        Name = s.Member?.User?.DisplayName ?? s.UserId,
                        AvatarUrl = s.Member?.User?.AvatarUrl ?? "https://cdn.discordapp.com/embed/avatars/0.png",
                        IsMuted = s.Mute || s.SelfMute,
                        IsDeafened = s.Deaf || s.SelfDeaf
                    });
                }
            }
        }

        private bool HasPermission(Channel channel, GuildMember member, System.Collections.Generic.List<Role> roles, long permissionBit)
        {
            // 2. Compute Base Permissions
            long permissions = ComputeBasePermissions(member, roles);

            // 3. Administrator Logic (0x8)
            if ((permissions & 0x8) == 0x8) return true;

            // 4. Permission Overwrites
            if (channel.PermissionOverwrites != null)
            {
                // @everyone overwrite
                var everyoneOverwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == _currentServer.Id);
                if (everyoneOverwrite != null)
                {
                    permissions &= ~long.Parse(everyoneOverwrite.Deny);
                    permissions |= long.Parse(everyoneOverwrite.Allow);
                }

                // Role overwrites
                long roleAllow = 0;
                long roleDeny = 0;
                foreach (var roleId in member.Roles)
                {
                    var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == roleId);
                    if (overwrite != null)
                    {
                        roleAllow |= long.Parse(overwrite.Allow);
                        roleDeny |= long.Parse(overwrite.Deny);
                    }
                }
                permissions &= ~roleDeny;
                permissions |= roleAllow;

                // Member overwrite
                var memberOverwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == member.User.Id);
                if (memberOverwrite != null)
                {
                    permissions &= ~long.Parse(memberOverwrite.Deny);
                    permissions |= long.Parse(memberOverwrite.Allow);
                }
            }

            return (permissions & permissionBit) == permissionBit;
        }

        private long ComputeBasePermissions(GuildMember member, System.Collections.Generic.List<Role> roles)
        {
            long permissions = 0;

            // @everyone role (ID == Guild ID)
            var everyoneRole = roles.FirstOrDefault(r => r.Id == _currentServer.Id);
            if (everyoneRole != null)
            {
                permissions |= long.Parse(everyoneRole.Permissions);
            }

            // Member roles
            foreach (var roleId in member.Roles)
            {
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                if (role != null)
                {
                    permissions |= long.Parse(role.Permissions);
                }
            }
            
            return permissions;
        }

        private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelList.SelectedItem is ChannelViewItem item)
            {
                // Prevent selection of categories
                if (item.IsCategory)
                {
                     return;
                }

                // Prevent selection of Voice Channels (Type 2) for now
                if (item.Channel.Type == 2)
                {
                    // Maybe TODO: Join voice?
                    return;
                }

                // Navigate to Chat
                var context = new ChatContext
                {
                    Service = _discordService,
                    Channel = item.Channel,
                    CanWrite = item.CanWrite
                };

                ChatFrame.Navigate(typeof(ChatPage), context);

                // Clear chat backstack to avoid keeping old ChatPage instances in memory
                while (ChatFrame.BackStack.Count > 0)
                {
                    ChatFrame.BackStack.RemoveAt(0);
                }

                _discordService.Http.AddToRecentChannels(item.Channel);
            }
        }
    }

    public class VoiceUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
    }

    public class ChannelViewItem : System.ComponentModel.INotifyPropertyChanged
    {
        public Channel Channel { get; set; }
        public bool IsCategory { get; set; }
        public bool CanWrite { get; set; } = true;
        public ObservableCollection<VoiceUser> VoiceUsers { get; } = new ObservableCollection<VoiceUser>();
        public string Name => Channel?.Name?.ToUpper();
        
        public ChannelViewItem()
        {
            VoiceUsers.CollectionChanged += (s, e) => 
            {
                OnPropertyChanged(nameof(UserCount));
                OnPropertyChanged(nameof(UserLimitVisibility));
                OnPropertyChanged(nameof(UserCountText));
            };
        }

        public int UserCount => VoiceUsers.Count;
        
        public bool HasUserLimit => Channel?.UserLimit > 0;
        
        public Microsoft.UI.Xaml.Visibility UserLimitVisibility => (Channel?.Type == 2 && (VoiceUsers.Count > 0 || HasUserLimit)) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public string UserCountText 
        {
            get
            {
                if (Channel == null) return "";
                if (Channel.UserLimit > 0) return $"{VoiceUsers.Count} / {Channel.UserLimit}";
                return VoiceUsers.Count > 0 ? VoiceUsers.Count.ToString() : "";
            }
        }
        
        public string IconGlyph
        {
            get
            {
                if (IsCategory) return "";
                if (Channel?.Type == 2) return "\uE720"; // Volume/Speaker Icon
                return "\uE8BD"; // Hash Icon for Text
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class ChannelItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ChannelTemplate { get; set; }
        public DataTemplate CategoryTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return SelectTemplateCore(item, null);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ChannelViewItem viewItem)
            {
                return viewItem.IsCategory ? CategoryTemplate : ChannelTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }

    // Helper class to pass data to the page
    public class ServerContext
    {
        public DiscordService Service { get; set; }
        public Server Server { get; set; }
        public string TargetChannelId { get; set; }
    }
}
