using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NativeDiscord.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NativeDiscord.Views
{
    public sealed partial class FriendsListPage : Page
    {
        private List<Relationship> _allRelationships = new List<Relationship>();
        public ObservableCollection<Relationship> FilteredRelationships { get; } = new ObservableCollection<Relationship>();
        public ObservableCollection<Relationship> ActiveFriends { get; } = new ObservableCollection<Relationship>();

        // We need the service to send requests or refresh
        private Services.DiscordService _discordService;

        private string _currentFilter = "Online";

        private Microsoft.UI.Xaml.DispatcherTimer _timer;

        public FriendsListPage()
        {
            this.InitializeComponent();
            
            // CRITICAL: Disable caching to allow this page to be GC'd
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            
            _timer = new Microsoft.UI.Xaml.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Services.DiscordService service)
            {
                _discordService = service;
                _discordService.PresenceUpdated += OnPresenceUpdated;
                LoadDataAsync();
            }
            _timer.Start();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            
            // CRITICAL: Clean up to prevent memory leaks
            if (_discordService != null)
            {
                _discordService.PresenceUpdated -= OnPresenceUpdated;
                _discordService = null;
            }
            
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            // Clear all collections to release references
            FilteredRelationships.Clear();
            ActiveFriends.Clear();
            _allRelationships?.Clear();
            _allRelationships = null;
            
            // Clear ListView sources to help release UI elements
            if (FriendsListView != null)
            {
                FriendsListView.ItemsSource = null;
            }
            if (ActiveNowList != null)
            {
                ActiveNowList.ItemsSource = null;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            if (ActiveFriends == null) return;
            foreach (var rel in ActiveFriends)
            {
                if (rel.PrimaryActivity != null && rel.PrimaryActivity.Timestamps != null && rel.PrimaryActivity.Timestamps.Start.HasValue)
                {
                    long startUnix = rel.PrimaryActivity.Timestamps.Start.Value;
                    DateTimeOffset startTime = DateTimeOffset.FromUnixTimeMilliseconds(startUnix);
                    TimeSpan diff = DateTimeOffset.UtcNow - startTime;
                    
                    // Format: "24:05" or "02:45:10"
                    if (diff.TotalHours >= 1)
                         rel.ElapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", diff.Hours, diff.Minutes, diff.Seconds);
                    else
                         rel.ElapsedTime = string.Format("{0:D2}:{1:D2}", diff.Minutes, diff.Seconds);
                }
                else
                {
                    rel.ElapsedTime = "";
                }
            }
        }

        private void OnPresenceUpdated(object sender, NativeDiscord.Models.PresenceUpdate e)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                // Only refresh if the update affects a friend in our list
                // For simplicity, just re-apply filter which is fast enough
                ApplyFilter(_currentFilter);
            });
        }

        private async void LoadDataAsync()
        {
            if (_discordService == null) return;
            try
            {
                // Use the shared list from DiscordService because it receives Gateway updates.
                // Re-fetching via HTTP would create stale objects.
                if (_discordService.Relationships == null)
                {
                     // Fallback if not initialized (though MainWindow usually does it)
                     var rels = await _discordService.Http.GetRelationshipsAsync();
                     // We can't easily set _discordService.Relationships since setter is private, 
                     // but we should assume it's there or just use local. 
                     // Ideally we'd warn. For now let's just use what we get.
                     _allRelationships = rels ?? new List<Relationship>();
                }
                else
                {
                    _allRelationships = _discordService.Relationships;
                }

                ApplyFilter(_currentFilter);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading relationships: " + ex.Message);
            }
        }

        private void Tab_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string tag = rb.Content.ToString();
                ApplyFilter(tag);
            }
        }

        private void BtnAddFriend_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ApplyFilter("Add Friend");
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(_currentFilter);
        }

        private void ApplyFilter(string filter)
        {
            _currentFilter = filter;
            
            // UI Toggle
            if (filter == "Add Friend")
            {
                ListViewContainer.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                AddFriendView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                BtnOnline.IsChecked = false;
                BtnAll.IsChecked = false;
                BtnPending.IsChecked = false;
                BtnBlocked.IsChecked = false;
            }
            else
            {
                ListViewContainer.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                AddFriendView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                
                // Get Search Text
                string query = SearchBox?.Text?.Trim().ToLower() ?? "";

                // Filter List
                FilteredRelationships.Clear();
                int typeFilter = -1; 
                
                // Types: 1=Friend, 2=Blocked, 3=Incoming, 4=Outgoing
                switch (filter)
                {
                    case "Online": 
                        typeFilter = 1; 
                        break;
                    case "All": 
                        typeFilter = 1; 
                        break;
                    case "Pending": 
                        typeFilter = 34; // 3 or 4
                        break;
                    case "Blocked": 
                        typeFilter = 2; 
                        break;
                }

                int count = 0;
                foreach (var rel in _allRelationships)
                {
                    bool match = false;
                    
                    if (filter == "Online")
                    {
                        // Check real presence using the new helper
                        if (rel.IsOnline && rel.User != null) match = true; 
                    }
                    else if (filter == "Pending")
                    {
                        if (rel.Type == 3 || rel.Type == 4) match = true;
                    }
                    else if (typeFilter == rel.Type)
                    {
                        match = true;
                    }

                    // Apply Search Query
                    if (match && !string.IsNullOrEmpty(query))
                    {
                        string name = rel.User.DisplayName?.ToLower() ?? "";
                        string username = rel.User.Username?.ToLower() ?? "";
                        if (!name.Contains(query) && !username.Contains(query))
                        {
                            match = false;
                        }
                    }

                    if (match && rel.User != null)
                    {
                        FilteredRelationships.Add(rel);
                        count++;
                    }
                }
                
                // Update Active Now
                ActiveFriends.Clear();
                foreach (var rel in _allRelationships)
                {
                    // Type 1 = Friend. Must have a "real" activity (not custom status).
                    // Activity Types: 0=Game, 1=Streaming, 2=Listening, 3=Watching, 4=Custom (exclude), 5=Competing
                    if (rel.Type == 1 && rel.Activities != null && rel.Activities.Count > 0)
                    {
                        // Find the first non-custom-status activity
                        foreach (var act in rel.Activities)
                        {
                            // Only add if it's a real activity type (not custom status type 4)
                            if (act.Type == 0 || act.Type == 1 || act.Type == 2 || act.Type == 3 || act.Type == 5)
                            {
                                ActiveFriends.Add(rel);
                                break; // Only add once per friend
                            }
                        }
                    }
                }

                
                ListHeaderLabel.Text = $"{filter.ToUpper()} - {count}";
                FriendsListView.ItemsSource = FilteredRelationships;
                
                // Toggle Active Now Visibility
                if (ActiveNowEmptyState != null && ActiveNowList != null)
                {
                    bool hasActive = ActiveFriends.Count > 0;
                    ActiveNowEmptyState.Visibility = hasActive ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
                    ActiveNowList.Visibility = hasActive ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
                }
            }
        }

        private async void SendFriendRequest_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
             if (_discordService == null) return;
             string text = AddFriendInput.Text;
             if (string.IsNullOrWhiteSpace(text)) return;
             
             // Simple parsing for Username vs Username#Discriminator (Old style)
             // New style: just username
             
             try
             {
                 await _discordService.Http.SendFriendRequestAsync(text.Trim());
                 
                 // Show success dialog or clear input
                 AddFriendInput.Text = "";
                 
                 // Ideally show a "Success" toast
                 var dialog = new ContentDialog
                 {
                     Title = "Friend Request Sent",
                     Content = $"Sent request to {text}",
                     CloseButtonText = "Okay",
                     XamlRoot = this.XamlRoot
                 };
                 await dialog.ShowAsync();
             }
             catch
             {
                 var dialog = new ContentDialog
                 {
                     Title = "Failed",
                     Content = $"Could not find user '{text}'. Check capitalization/spelling.",
                     CloseButtonText = "Okay",
                     XamlRoot = this.XamlRoot
                 };
                 await dialog.ShowAsync();
             }
        }
    }
}
