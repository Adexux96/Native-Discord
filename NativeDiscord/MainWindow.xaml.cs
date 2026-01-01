using System;
using System.Linq; // Added for Any()
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using NativeDiscord.Models;

namespace NativeDiscord
{
    public sealed partial class MainWindow : Window
    {
        private NativeDiscord.Services.DiscordService _discordService;
        public System.Collections.ObjectModel.ObservableCollection<SearchResultItem> SearchResults { get; } = new System.Collections.ObjectModel.ObservableCollection<SearchResultItem>();


        private bool _suppressNavigation = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Native Discord";
            
            // Custom Title Bar functionality
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            _discordService = new NativeDiscord.Services.DiscordService();
            _discordService.Dispatcher = this.DispatcherQueue;
            _discordService.NavigationRequested += OnNavigationRequested;

            this.Activated += MainWindow_Activated;
        }

        private async void OnNavigationRequested(object sender, string channelId)
        {
            try
            {
                // 1. Try to get channel (cache or fetch)
                var channel = _discordService.GetCachedChannel(channelId);
                if (channel == null)
                {
                    try
                    {
                        channel = await _discordService.Http.GetChannelAsync(channelId);
                        if (channel != null)
                        {
                            // Manually cache it if we can, or just use it. 
                            // RequestChannel uses UpdateChannelCache internally, but we can't access it.
                            // But we can just use the object.
                            // If we want to cache, we can call RequestChannel(id) which is void, 
                            // but we already fetched it.
                            // Let's assume fetching is enough for now.
                        }
                    }
                    catch { }
                }

                if (channel == null) return; // Failed to resolve

                // 2. Navigate based on type
                if (!string.IsNullOrEmpty(channel.GuildId))
                {
                    // Guild Channel
                    var guild = _discordService.Guilds?.FirstOrDefault(g => g.Id == channel.GuildId);
                    if (guild != null)
                    {
                        // Switch Server List Selection silently
                        _suppressNavigation = true;
                        ServerList.SelectedItem = guild;
                        _suppressNavigation = false;

                        // Navigate with Context
                        var context = new Views.ServerContext
                        {
                            Service = _discordService,
                            Server = guild,
                            TargetChannelId = channelId
                        };
                        ContentFrame.Navigate(typeof(Views.ServerPage), context);
                    }
                }
                else
                {
                    // DM or Group DM
                    _discordService.Http.AddToRecentChannels(channel);
                    ContentFrame.Navigate(typeof(Views.FriendsPage), new Views.FriendsPageNavigationArgs
                    {
                        Service = _discordService,
                        TargetChannel = channel
                    });
                    
                    // Clear Server Selection
                    _suppressNavigation = true;
                    ServerList.SelectedItem = null;
                    _suppressNavigation = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
            }
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= MainWindow_Activated; // Run once
            
            // Show Login Page initially
            ContentFrame.Navigate(typeof(Views.LoginPage));
            
            if (ContentFrame.Content is Views.LoginPage loginPage)
            {
                loginPage.TokenReceived += async (s, token) =>
                {
                    loginPage.Cleanup();
                    ContentFrame.BackStack.Clear();
                    
                    try
                    {
                        await _discordService.LoginAsync(token);
                        await _discordService.InitializeDataAsync();
                        
                        // Populate Server List (ItemsSource binding)
                        if (_discordService.Guilds != null)
                        {
                            ServerList.ItemsSource = _discordService.Guilds;
                        }
                        
                        // Navigate to Home (Friends) by default
                        ContentFrame.Navigate(typeof(Views.FriendsPage), _discordService);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
                    }
                };
            }
        }

        private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressNavigation) return;

            if (ServerList.SelectedItem is Server server)
            {
                // Reset Home Button State if needed (visuals)
                // Navigate
                var context = new Views.ServerContext { Service = _discordService, Server = server };
                ContentFrame.Navigate(typeof(Views.ServerPage), context);
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ServerList.SelectedItem = null; // Deselect server
            ContentFrame.Navigate(typeof(Views.FriendsPage), _discordService);
        }

        // Search Implementation
        private void RootGrid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.K && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                ShowSearch();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (SearchOverlay.Visibility == Visibility.Visible)
                {
                    HideSearch();
                    e.Handled = true;
                }
            }
        }

        public void ShowSearch()
        {
            SearchOverlay.Visibility = Visibility.Visible;
            PopulateRecents();
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void HideSearch()
        {
            SearchOverlay.Visibility = Visibility.Collapsed;
            SearchBox.Text = "";
        }

        private void PopulateRecents()
        {
            try
            {
                SearchResults.Clear();
                if (_discordService?.Http?.RecentChannels != null)
                {
                    // Create a safe copy to iterate
                    var recents = _discordService.Http.RecentChannels.ToList();
                    foreach (var channel in recents)
                    {
                        try
                        {
                            var item = CreateSearchItem(channel, "Recent");
                            if (item != null) SearchResults.Add(item);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating search item: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating recents: {ex}");
            }
        }

        private SearchResultItem CreateSearchItem(Channel channel, string typeContext)
        {
            if (channel == null) return null;

            // Try to find Guild Name
            string subtitle = "Direct Message";
            string iconUrl = null;
            string glyph = null;

            if (!string.IsNullOrEmpty(channel.GuildId))
            {
                var guild = _discordService.Guilds?.Find(g => g.Id == channel.GuildId);
                subtitle = guild?.Name ?? "Server Channel";
                
                // Icon Logic
                if (channel.Type == 2) glyph = "\uE767"; // Volume (Voice)
                else glyph = "\uE8BD"; // Comment (Text)
            }
            else if (channel.Recipients != null && channel.Recipients.Count > 0)
            {
                 var user = channel.Recipients[0];
                 subtitle = "Direct Message"; // Matches user feedback to label correctly
                 iconUrl = user.AvatarUrl; // Use User Avatar
                 typeContext = "User"; // Update Label
            }

            return new SearchResultItem
            {
                Id = channel.Id,
                Title = string.IsNullOrEmpty(channel.Name) ? (channel.Recipients?.Count > 0 ? channel.Recipients[0].Username : subtitle) : channel.Name,
                Subtitle = subtitle,
                Type = typeContext == "Recent" ? (subtitle == "Direct Message" ? "User" : "Channel") : typeContext, 
                OriginalObject = channel,
                IconUrl = iconUrl,
                Glyph = glyph
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(query))
            {
                PopulateRecents();
                return;
            }

            SearchResults.Clear();
            
            // Search DMs
            // Search Friends (Relationships)
            if (_discordService?.Relationships != null)
            {
                foreach (var rel in _discordService.Relationships)
                {
                    // Type 1 is Friend
                    if (rel.Type == 1 && rel.User != null && rel.User.Username.ToLower().Contains(query))
                    {
                         SearchResults.Add(new SearchResultItem
                         {
                             Id = rel.User.Id,
                             Title = rel.User.Username,
                             Subtitle = rel.User.GlobalName ?? "Friend",
                             Type = "User",
                             OriginalObject = rel.User, // Pass User object
                             IconUrl = rel.User.AvatarUrl
                         });
                    }
                }
            }
            
            if (_discordService.Guilds != null)
            {
                foreach (var guild in _discordService.Guilds)
                {
                    if (guild.Name.ToLower().Contains(query))
                    {
                        SearchResults.Add(new SearchResultItem 
                        {
                            Id = guild.Id,
                            Title = guild.Name,
                            Subtitle = "Server",
                            Type = "Server",
                            OriginalObject = guild,
                            IconUrl = guild.IconUrl
                        });
                    }
                }
            }
            
            // Simple history search
             if (_discordService?.Http?.RecentChannels != null)
            {
                foreach (var channel in _discordService.Http.RecentChannels)
                {
                    if ((channel.Name != null && channel.Name.ToLower().Contains(query)) ||
                        (channel.Recipients != null && channel.Recipients.Exists(u => u.Username.ToLower().Contains(query))))
                    {
                         // Avoid duplicates if we already added it via some other means
                         if (!SearchResults.Any(x => x.Id == channel.Id))
                            SearchResults.Add(CreateSearchItem(channel, "Recent"));
                    }
                }
            }
        }

        private void SearchList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is SearchResultItem item)
            {
                HideSearch();

                if (item.Type == "Server")
                {
                     var server = item.OriginalObject as Server;
                     ServerList.SelectedItem = server; // Triggers navigation
                }
                else if (item.Type == "Channel")
                {
                    var channel = item.OriginalObject as Channel;
                    // Check if server channel
                    if (!string.IsNullOrEmpty(channel.GuildId))
                    {
                        // Switch Server
                        var server = _discordService.Guilds?.Find(g => g.Id == channel.GuildId);
                        if (server != null)
                        {
                            ServerList.SelectedItem = server;
                            
                            // We need to navigate to the specific channel.
                            // ServerPage handles loading channels. We might need a mechanism to 'RequestNavigateToChannel'.
                            // For now, just switching server is a good start, but we can try to pass a parameter.
                            // Since ServerPage loads async, this is tricky.
                            // Lets just switch server for now or try to navigate if we are already there.
                            
                            // Improved: Pass navigation hint? 
                            // Creating a simplified solution: Just switch server. 
                            // Users can click channel. 
                        }
                    }
                    else
                    {
                        // DM
                        _discordService.Http.AddToRecentChannels(channel);
                        ContentFrame.Navigate(typeof(Views.FriendsPage), new Views.FriendsPageNavigationArgs 
                        { 
                            Service = _discordService,
                            TargetChannel = channel
                        });
                    }
                }
                else if (item.Type == "User")
                {
                    if (item.OriginalObject is Models.User user)
                    {
                         ContentFrame.Navigate(typeof(Views.FriendsPage), new Views.FriendsPageNavigationArgs 
                         { 
                             Service = _discordService, 
                             TargetUserId = user.Id 
                         });
                    }
                    else if (item.OriginalObject is Models.Channel channel)
                    {
                         // DM from Recent History
                         _discordService.Http.AddToRecentChannels(channel);
                         ContentFrame.Navigate(typeof(Views.FriendsPage), new Views.FriendsPageNavigationArgs 
                         { 
                            Service = _discordService,
                            TargetChannel = channel
                         });
                    }
                }
            }
        }

        public Visibility ToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
    }

    public class BoolToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
