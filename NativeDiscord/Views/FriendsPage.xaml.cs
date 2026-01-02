using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NativeDiscord.Models;
using NativeDiscord.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace NativeDiscord.Views
{
    public sealed partial class FriendsPage : Page
    {
        public ObservableCollection<User> Friends { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<DMChannelViewModel> PrivateChannels { get; set; } = new ObservableCollection<DMChannelViewModel>();
        private DiscordService _discordService;

        public FriendsPage()
        {
            this.InitializeComponent();

            // Prevent Frame cache from keeping old ChatPages alive.
            HomeContentFrame.CacheSize = 0;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is DiscordService service)
            {
                _discordService = service;
                await LoadDataAsync();

                // Populate User Footer
                if (_discordService.CurrentUser != null)
                {
                    CurrentUserDisplayName.Text = _discordService.CurrentUser.DisplayName ?? _discordService.CurrentUser.Username;
                    CurrentUserUsername.Text = _discordService.CurrentUser.Username;

                    if (!string.IsNullOrEmpty(_discordService.CurrentUser.AvatarUrl))
                    {
                        CurrentUserAvatar.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(_discordService.CurrentUser.AvatarUrl));
                    }
                }

                // Default Navigation: Friends List
                HomeContentFrame.Navigate(typeof(FriendsListPage), _discordService);
                HomeContentFrame.BackStack.Clear();
            }
            else if (e.Parameter is FriendsPageNavigationArgs args)
            {
                _discordService = args.Service;
                await LoadDataAsync();

                // Populate User Footer
                if (_discordService.CurrentUser != null)
                {
                    CurrentUserDisplayName.Text = _discordService.CurrentUser.DisplayName ?? _discordService.CurrentUser.Username;
                    CurrentUserUsername.Text = _discordService.CurrentUser.Username;

                    if (!string.IsNullOrEmpty(_discordService.CurrentUser.AvatarUrl))
                    {
                        CurrentUserAvatar.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(_discordService.CurrentUser.AvatarUrl));
                    }
                }

                if (args.TargetChannel != null)
                {
                    var matchingDM = PrivateChannels.FirstOrDefault(d => d.Channel.Id == args.TargetChannel.Id);
                    if (matchingDM != null) DMList.SelectedItem = matchingDM;
                }
                else if (!string.IsNullOrEmpty(args.TargetUserId))
                {
                    // Find DM with this user
                    var matchingDM = PrivateChannels.FirstOrDefault(d => d.Channel.Recipients != null && d.Channel.Recipients.Any(u => u.Id == args.TargetUserId));
                    if (matchingDM != null)
                    {
                        DMList.SelectedItem = matchingDM;
                    }
                    else
                    {
                        // Fallback to friends list if DM not found
                        HomeContentFrame.Navigate(typeof(FriendsListPage), _discordService);
                        HomeContentFrame.BackStack.Clear();
                    }
                }
                else
                {
                    // Fallback
                    HomeContentFrame.Navigate(typeof(FriendsListPage), _discordService);
                    HomeContentFrame.BackStack.Clear();
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            // Ensure nested Frame doesn't keep pages around.
            HomeContentFrame.BackStack.Clear();
            HomeContentFrame.Content = null;

            DisposePrivateChannelViewModels();

            // Help avatar images get collected.
            CurrentUserAvatar.ImageSource = null;
        }

        private async Task LoadDataAsync()
        {
            await LoadDMsAsync();
            // We don't strictly need LoadFriendsAsync for the Sidebar anymore, as Friends logic is moved to FriendsListPage
        }

        private async Task LoadDMsAsync()
        {
            try
            {
                var dms = await _discordService.Http.GetPrivateChannelsAsync();

                // IMPORTANT: DMChannelViewModel subscribes to PresenceUpdated.
                // If we replace the list without disposing, old VMs remain rooted forever.
                DisposePrivateChannelViewModels();

                PrivateChannels.Clear();
                foreach (var dm in dms)
                {
                    var vm = new DMChannelViewModel(dm, _discordService);
                    PrivateChannels.Add(vm);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching DMs: {ex.Message}");
            }
        }

        private void DisposePrivateChannelViewModels()
        {
            foreach (var vm in PrivateChannels)
            {
                if (vm is System.IDisposable d)
                {
                    try { d.Dispose(); } catch { }
                }
            }
        }

        private void DMList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DMList.SelectedItem is DMChannelViewModel dm)
            {
                // Navigate to ChatPage with Context
                var context = new ChatContext
                {
                    Service = _discordService,
                    Channel = dm.Channel,
                    CanWrite = true // DMs usually writable unless blocked
                };

                _discordService.Http.AddToRecentChannels(dm.Channel);

                HomeContentFrame.Navigate(typeof(ChatPage), context);
                HomeContentFrame.BackStack.Clear();
            }
        }

        private void FriendsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            DMList.SelectedItem = null; // Deselect DM
            HomeContentFrame.Navigate(typeof(FriendsListPage), _discordService);
            HomeContentFrame.BackStack.Clear();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainWindow is MainWindow mw)
            {
                mw.ShowSearch();
            }
        }
    }

    public class FriendsPageNavigationArgs
    {
        public DiscordService Service { get; set; }
        public string TargetUserId { get; set; }
        public Channel TargetChannel { get; set; }
    }
}
