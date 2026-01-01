using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NativeDiscord.Models;
using NativeDiscord.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NativeDiscord.Views
{
    public sealed partial class ChatPage : Page, System.ComponentModel.INotifyPropertyChanged
    {
        private DiscordService _discordService;
        private Channel _currentChannel;
        // Use ViewModel for UI logic
        public ObservableCollection<MessageViewModel> Messages { get; } = new ObservableCollection<MessageViewModel>();

        // Typing Logic
        private DispatcherTimer _typingTimer;
        private Dictionary<string, (string Name, DateTime Time)> _typingUsers = new Dictionary<string, (string Name, DateTime Time)>();
        
        // Exposed Context to be bound in XAML header
        public ChatContext Context { get; private set; }
        public string CurrentGuildId => _currentChannel?.GuildId;

        private User _sidebarUser;
        public User SidebarUser
        {
            get => _sidebarUser;
            set
            {
                if (_sidebarUser != value)
                {
                    _sidebarUser = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(SidebarUser)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public ChatPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ChatContext context)
            {
                Context = context; // Store context for binding header
                _discordService = context.Service;
                _currentChannel = context.Channel;
                
                // Subscribe to real-time messages
                _discordService.MessageReceived += OnMessageReceived;
                _discordService.MessageUpdated += OnMessageUpdated;
                _discordService.MessageDeleted += OnMessageDeleted;
                _discordService.UserTyping += OnUserTyping;
                _discordService.MessageReactionAdded += OnMessageReactionAdded;
                _discordService.MessageReactionRemoved += OnMessageReactionRemoved;
                _discordService.UserResolved += OnUserResolved;

                ChannelNameBlock.Text = _currentChannel.Name;
                
                // Sidebar Logic: Pick the other person if DM, or self if empty?
                // For DM (Type 1), Recipients list has the other person.
                if (_currentChannel.Recipients != null && _currentChannel.Recipients.Count > 0)
                {
                     SidebarUser = _currentChannel.Recipients[0];
                }
                else
                {
                    // Fallback to current user if group or server? Or just null and hide.
                    SidebarUser = _discordService.CurrentUser;
                }

                if (context.CanWrite)
                {
                    MessageInput.IsEnabled = true;
                    if (_currentChannel.Type == 1) // DM
                         MessageInput.PlaceholderText = $"Message @{_currentChannel.Name}"; // Usually DM name is user name
                    else
                         MessageInput.PlaceholderText = $"Message #{_currentChannel.Name}";
                }
                else
                {
                    MessageInput.IsEnabled = false;
                    MessageInput.PlaceholderText = "You do not have permission to send messages in this channel.";
                }

                await LoadMessagesAsync();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (_discordService != null)
            {
                _discordService.MessageReceived -= OnMessageReceived;
                _discordService.MessageUpdated -= OnMessageUpdated;
                _discordService.MessageDeleted -= OnMessageDeleted;
                _discordService.UserTyping -= OnUserTyping;
                _discordService.MessageReactionAdded -= OnMessageReactionAdded;
                _discordService.MessageReactionRemoved -= OnMessageReactionRemoved;
                _discordService.UserResolved -= OnUserResolved;
                _discordService.ChannelResolved -= OnChannelResolved;
            }
            _typingTimer?.Stop();
        }

        private void OnUserResolved(object sender, string userId)
        {
             DispatcherQueue.TryEnqueue(() => 
             {
                 foreach (var vm in Messages)
                 {
                      bool shouldRefresh = false;
                      if (vm.Message.Content != null && vm.Message.Content.Contains(userId)) shouldRefresh = true;
                      
                      // Scan embeds
                      if (!shouldRefresh && vm.Message.Embeds != null)
                      {
                          foreach (var e in vm.Message.Embeds)
                          {
                              if ((e.Description != null && e.Description.Contains(userId)) ||
                                  (e.Title != null && e.Title.Contains(userId)))
                              {
                                  shouldRefresh = true;
                                  break;
                              }
                          }
                      }

                      if (shouldRefresh)
                      {
                          vm.RefreshContent();
                      }
                 }
             });
        }

        private void OnChannelResolved(object sender, string channelId)
        {
             DispatcherQueue.TryEnqueue(() => 
             {
                 foreach (var vm in Messages)
                 {
                      bool shouldRefresh = false;
                      if (vm.Message.Content != null && vm.Message.Content.Contains(channelId)) shouldRefresh = true;
                      
                      // Scan embeds
                      if (!shouldRefresh && vm.Message.Embeds != null)
                      {
                          foreach (var e in vm.Message.Embeds)
                          {
                              if ((e.Description != null && e.Description.Contains(channelId)) ||
                                  (e.Title != null && e.Title.Contains(channelId)))
                              {
                                  shouldRefresh = true;
                                  break;
                              }
                          }
                      }

                      if (shouldRefresh)
                      {
                          vm.RefreshContent();
                      }
                 }
             });
        }

        private void OnUserTyping(object sender, Models.TypingStartPayload e)
        {
            if (_currentChannel == null || e.ChannelId != _currentChannel.Id) return;
            if (e.UserId == _discordService.CurrentUser?.Id) return;

            DispatcherQueue.TryEnqueue(() => 
            {
                string name = "Someone";
                if (e.Member?.User != null)
                     name = e.Member.User.DisplayName;
                else
                {
                     // Try relationships or cache
                     var rel = _discordService.Relationships?.FirstOrDefault(r => r.Id == e.UserId || (r.User != null && r.User.Id == e.UserId));
                     if (rel?.User != null) name = rel.User.DisplayName;
                }

                _typingUsers[e.UserId] = (name, DateTime.Now);
                UpdateTypingIndicator();
                StartTypingTimer();
            });
        }

        private void StartTypingTimer()
        {
            if (_typingTimer == null)
            {
                _typingTimer = new DispatcherTimer();
                _typingTimer.Interval = TimeSpan.FromSeconds(1);
                _typingTimer.Tick += (s, e) => UpdateTypingIndicator();
            }
            if (!_typingTimer.IsEnabled) _typingTimer.Start();
        }

        private void UpdateTypingIndicator()
        {
            // Remove old
            var now = DateTime.Now;
            var expired = _typingUsers.Where(kv => (now - kv.Value.Time).TotalSeconds > 10).Select(kv => kv.Key).ToList();
            foreach (var key in expired) _typingUsers.Remove(key);

            if (_typingUsers.Count == 0)
            {
                TypingIndicatorPanel.Visibility = Visibility.Collapsed;
                _typingTimer?.Stop();
                return;
            }

            TypingIndicatorPanel.Visibility = Visibility.Visible;
            var names = _typingUsers.Values.Select(v => v.Name).Distinct().ToList();
            
            if (names.Count == 1)
            {
                TypingIndicatorText.Text = $"{names[0]} is typing...";
            }
            else if (names.Count == 2)
            {
                TypingIndicatorText.Text = $"{names[0]} and {names[1]} are typing...";
            }
            else if (names.Count == 3)
            {
                 TypingIndicatorText.Text = $"{names[0]}, {names[1]}, and {names[2]} are typing...";
            }
            else
            {
                TypingIndicatorText.Text = "Several people are typing...";
            }
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                if (_currentChannel != null && msg.ChannelId == _currentChannel.Id)
                {
                    // De-duplication check
                    if (Messages.Any(m => m.Message.Id == msg.Id))
                        return;

                    // Clear typing status for this user
                    if (_typingUsers.ContainsKey(msg.Author.Id))
                    {
                        _typingUsers.Remove(msg.Author.Id);
                        UpdateTypingIndicator();
                    }

                    bool showHeader = true;
                    bool showDateHeader = false;
                    string dateHeaderText = "";

                    if (Messages.Count > 0)
                    {
                        var lastMsg = Messages.Last();
                        
                        // Check for Date Header
                        if (lastMsg.Message.Timestamp.Date != msg.Timestamp.Date)
                        {
                            showDateHeader = true;
                            dateHeaderText = msg.Timestamp.ToString("D"); // Full date pattern
                            showHeader = true; // Always show user header on new date
                        }
                        else if (lastMsg.Message.Author.Id == msg.Author.Id)
                        {
                             var timeDiff = msg.Timestamp - lastMsg.Message.Timestamp;
                             if (timeDiff.TotalMinutes < 7)
                             {
                                 showHeader = false;
                             }
                        }
                    }
                    else
                    {
                        // First message ever
                        showDateHeader = true;
                        dateHeaderText = msg.Timestamp.ToString("D");
                    }

                    var vm = new MessageViewModel 
                    { 
                        Message = msg, 
                        ShowHeader = showHeader,
                        ShowDateHeader = showDateHeader,
                        DateHeaderText = dateHeaderText,

                        CanModify = msg.Author.Id == _discordService.CurrentUser?.Id,
                        Service = _discordService,
                        CurrentGuildId = CurrentGuildId
                    };
                    vm.InitializeWrappers();
                    Messages.Add(vm);
                    MessagesList.ScrollIntoView(vm);
                }
            });
        }

        private void OnMessageUpdated(object sender, Message msg)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                var vm = Messages.FirstOrDefault(m => m.Message.Id == msg.Id);
                if (vm != null)
                {
                    // Update content
                    vm.Message.Content = msg.Content;
                    vm.Message.EditedTimestamp = msg.EditedTimestamp;
                    vm.RefreshContent();
                }
            });
        }

        private void OnMessageDeleted(object sender, MessageDeletedPayload payload)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                var vm = Messages.FirstOrDefault(m => m.Message.Id == payload.Id);
                if (vm != null)
                {
                    Messages.Remove(vm);
                    // Regrouping might be needed here, but for now simple removal is okay
                }
            });
        }

        private void OnMessageReactionAdded(object sender, MessageReactionUpdatePayload e)
        {
            if (_currentChannel == null || e.ChannelId != _currentChannel.Id) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                var vm = Messages.FirstOrDefault(m => m.Message.Id == e.MessageId);
                if (vm != null)
                {
                    if (vm.Message.Reactions == null)
                        vm.Message.Reactions = new List<Reaction>();

                    var existing = vm.Message.Reactions.FirstOrDefault(r => 
                        (r.Emoji.Id == e.Emoji.Id && r.Emoji.Name == e.Emoji.Name) || 
                        (string.IsNullOrEmpty(r.Emoji.Id) && string.IsNullOrEmpty(e.Emoji.Id) && r.Emoji.Name == e.Emoji.Name));

                    if (existing != null)
                    {
                        existing.Count++;
                        if (e.UserId == _discordService.CurrentUser.Id) existing.Me = true;
                    }
                    else
                    {
                        vm.Message.Reactions.Add(new Reaction 
                        { 
                            Count = 1, 
                            Emoji = e.Emoji, 
                            Me = e.UserId == _discordService.CurrentUser.Id 
                        });
                        // Force list update for ItemsControl
                        vm.Message.Reactions = new List<Reaction>(vm.Message.Reactions);
                    }
                    vm.RefreshReactions();
                }
            });
        }

        private void OnMessageReactionRemoved(object sender, MessageReactionUpdatePayload e)
        {
            if (_currentChannel == null || e.ChannelId != _currentChannel.Id) return;

             DispatcherQueue.TryEnqueue(() =>
            {
                var vm = Messages.FirstOrDefault(m => m.Message.Id == e.MessageId);
                if (vm != null && vm.Message.Reactions != null)
                {
                    var existing = vm.Message.Reactions.FirstOrDefault(r => 
                        (r.Emoji.Id == e.Emoji.Id && r.Emoji.Name == e.Emoji.Name) || 
                        (string.IsNullOrEmpty(r.Emoji.Id) && string.IsNullOrEmpty(e.Emoji.Id) && r.Emoji.Name == e.Emoji.Name));

                    if (existing != null)
                    {
                        existing.Count--;
                        if (e.UserId == _discordService.CurrentUser.Id) existing.Me = false;

                        if (existing.Count <= 0)
                        {
                            vm.Message.Reactions.Remove(existing);
                            // Force list update for ItemsControl
                            vm.Message.Reactions = new List<Reaction>(vm.Message.Reactions);
                        }
                    }
                    vm.RefreshReactions();
                }
            });
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserProfileSidebar.Visibility == Visibility.Visible)
                UserProfileSidebar.Visibility = Visibility.Collapsed;
            else
                UserProfileSidebar.Visibility = Visibility.Visible;
        }

        private async System.Threading.Tasks.Task LoadMessagesAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                MessagesList.ItemsSource = null;
                Messages.Clear();

                var rawMessages = await _discordService.Http.GetMessagesAsync(_currentChannel.Id);
                
                // Process messages for grouping
                // API gives newest first. We need oldest first for chat log.
                var orderedMessages = ((System.Collections.Generic.IEnumerable<Message>)rawMessages).Reverse().ToList();

                MessageViewModel previous = null;

                foreach (var msg in orderedMessages)
                {
                    bool showHeader = true;
                    bool showDateHeader = false;
                    string dateHeaderText = "";

                    if (previous != null)
                    {
                        // Check for Date Header
                        if (previous.Message.Timestamp.Date != msg.Timestamp.Date)
                        {
                            showDateHeader = true;
                            dateHeaderText = msg.Timestamp.ToString("D");
                            showHeader = true; // Always show user header on new date
                        }
                        else if (msg.ReferencedMessage != null)
                        {
                            // Always show header for replies
                            showHeader = true;
                        }
                        else if (previous.Message.Author.Id == msg.Author.Id)
                        {
                            var timeDiff = msg.Timestamp - previous.Message.Timestamp;
                            if (timeDiff.TotalMinutes < 7)
                            {
                                showHeader = false;
                            }
                        }
                    }
                    else
                    {
                        // First message
                        showDateHeader = true;
                        dateHeaderText = msg.Timestamp.ToString("D");
                    }

                    var vm = new MessageViewModel 
                    { 
                        Message = msg, 
                        ShowHeader = showHeader,
                        ShowDateHeader = showDateHeader,
                        DateHeaderText = dateHeaderText,

                        CanModify = msg.Author.Id == _discordService.CurrentUser?.Id,
                        Service = _discordService,
                        CurrentGuildId = CurrentGuildId
                    };
                    vm.InitializeWrappers();
                    Messages.Add(vm);
                    previous = vm;
                }
                
                MessagesList.ItemsSource = Messages;
                
                // Scroll to bottom
                if (Messages.Count > 0)
                {
                    MessagesList.ScrollIntoView(Messages.Last());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        // Reply Logic
        private Message _replyingToMessage; // The message we are replying to

        private void StartReply(Message message)
        {
            _replyingToMessage = message;
            ReplyPreviewPanel.Visibility = Visibility.Visible;
            ReplyPreviewText.Text = $"Replying to {message.Author.DisplayName}";
            MessageInput.Focus(FocusState.Programmatic);
        }

        private void CancelReply()
        {
            _replyingToMessage = null;
            ReplyPreviewPanel.Visibility = Visibility.Collapsed;
        }

        private void ReplyCloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancelReply();
        }

        private void ReplyMenu_Click(object sender, RoutedEventArgs e)
        {
             if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
             {
                 StartReply(vm.Message);
             }
        }

        private async void MessageInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
                bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

                if (!isShiftDown)
                {
                    e.Handled = true; // Prevent newline insertion
                    
                    string content = MessageInput.Text;
                    if (string.IsNullOrWhiteSpace(content)) return;

                    try
                    {
                        var reference = _replyingToMessage != null ? new MessageReference 
                        { 
                            MessageId = _replyingToMessage.Id, 
                            ChannelId = _replyingToMessage.ChannelId, 
                            GuildId = _currentChannel.GuildId 
                        } : null;

                        await _discordService.Http.SendMessageAsync(_currentChannel.Id, content, reference);
                        
                        MessageInput.Text = string.Empty;
                        CancelReply(); // Reset reply state
                    }
                    catch (Exception ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
                    }
                }
            }
        }

        private void EditMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                vm.IsEditing = true;
            }
        }

        private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                try
                {
                    await _discordService.Http.DeleteMessageAsync(_currentChannel.Id, vm.Message.Id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting message: {ex.Message}");
                }
            }
        }

        private void ReactionMenu_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement Reaction Picker
        }

        private void MoreMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element) 
            {
                // Traverse up: Button -> StackPanel -> Border -> Grid
                DependencyObject parent = element.Parent; // StackPanel
                while (parent != null && !(parent is Grid))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }

                if (parent is Grid grid && grid.ContextFlyout != null)
                {
                    grid.ContextFlyout.ShowAt(grid);
                }
            }
        }

        private async void QuickReaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string emoji && element.DataContext is MessageViewModel vm)
            {
                try
                {
                    var reactionEmoji = new NativeDiscord.Models.Emoji { Name = emoji };
                    await _discordService.Http.AddReactionAsync(_currentChannel.Id, vm.Message.Id, reactionEmoji);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add quick reaction: {ex.Message}");
                }
            }
        }

        private void ForwardMenu_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement Forwarding
        }

        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(vm.Message.Content);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            }
        }

        private void CopyID_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                 var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                 package.SetText(vm.Message.Id);
                 Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            }
        }

        private void MessageGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                vm.IsHovered = true;
            }
        }

        private void MessageGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel vm)
            {
                vm.IsHovered = false;
            }
        }

        private void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Focus(FocusState.Programmatic);
                // Move cursor to end
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        private async void EditTextBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
                bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

                if (!isShiftDown)
                {
                    e.Handled = true;
                    if (sender is TextBox textBox && textBox.DataContext is MessageViewModel vm)
                    {
                        string newContent = textBox.Text;
                        if (string.IsNullOrWhiteSpace(newContent))
                        {
                            vm.IsEditing = false;
                            return;
                        }

                        if (newContent == vm.Message.Content)
                        {
                            vm.IsEditing = false;
                            return;
                        }

                        try
                        {
                            await _discordService.Http.EditMessageAsync(_currentChannel.Id, vm.Message.Id, newContent);
                            vm.IsEditing = false;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error editing message: {ex.Message}");
                        }
                    }
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (sender is TextBox textBox && textBox.DataContext is MessageViewModel vm)
                {
                    vm.IsEditing = false;
                }
            }
        }

        private async void OnReactionClicked(object sender, Reaction reaction)
        {
            if (sender is FrameworkElement element)
            {
                var vm = FindParentDataContext<MessageViewModel>(element);
                if (vm != null)
                {
                    try
                    {
                        if (reaction.Me)
                        {
                            await _discordService.Http.DeleteReactionAsync(vm.Message.ChannelId, vm.Message.Id, reaction.Emoji);
                        }
                        else
                        {
                            await _discordService.Http.AddReactionAsync(vm.Message.ChannelId, vm.Message.Id, reaction.Emoji);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to toggle reaction: {ex.Message}");
                    }
                }
            }
        }

        private TDataContext FindParentDataContext<TDataContext>(DependencyObject child) where TDataContext : class
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is TDataContext data)
                {
                    return data;
                }
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void MessageMenu_Opened(object sender, object e)
        {
            if (sender is MenuFlyout menu && menu.Target is FrameworkElement target && target.DataContext is MessageViewModel vm)
            {
                foreach (var item in menu.Items)
                {
                    if (item is MenuFlyoutItem menuItem)
                    {
                        if (menuItem.Name == "EditMenuItem" || menuItem.Name == "DeleteMenuItem")
                        {
                            menuItem.Visibility = vm.CanModify ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else if (menuItem.Name == "ReportMenuItem")
                        {
                            menuItem.Visibility = !vm.CanModify ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
        }
    }

    public class ChatContext
    {
        public DiscordService Service { get; set; }
        public Channel Channel { get; set; }
        public bool CanWrite { get; set; } = true;
    }

    public class MessageViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public DiscordService Service { get; set; }
        public Message Message { get; set; }
        public bool ShowHeader { get; set; }
        
        private bool _isHovered;
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    OnPropertyChanged(nameof(IsHovered));
                    OnPropertyChanged(nameof(ToolbarVisibility));
                    OnPropertyChanged(nameof(BackgroundBrush));
                }
            }
        }

        public Microsoft.UI.Xaml.Media.Brush BackgroundBrush => IsHovered 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(20, 4, 4, 5)) // Low opacity dark overlay
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                    OnPropertyChanged(nameof(ContentVisibility));
                    OnPropertyChanged(nameof(EditVisibility));
                    OnPropertyChanged(nameof(AttachmentsVisibility));
                    OnPropertyChanged(nameof(EmbedsVisibility));
                    OnPropertyChanged(nameof(ToolbarVisibility));
                }
            }
        }

        public bool CanModify { get; set; }
        public bool CanNotModify => !CanModify;
        
        // Proxy properties for easier binding
        public string Content
        {
            get => Message.Content;
            set
            {
                if (Message.Content != value)
                {
                    Message.Content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }
        public string TimestampFormatted => Message.TimestampFormatted;
        
        // Visibility Converters
        public Visibility HeaderVisibility => ShowHeader ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TimestampVisibility => !ShowHeader ? Visibility.Visible : Visibility.Collapsed; 
        
        public Visibility AttachmentsVisibility => (Message.HasAttachments && !IsEditing) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmbedsVisibility => (Message.HasEmbeds && !IsEditing) ? Visibility.Visible : Visibility.Collapsed; 

        public Visibility ContentVisibility => !IsEditing ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EditVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ToolbarVisibility => (IsHovered && !IsEditing) ? Visibility.Visible : Visibility.Collapsed;

        public string CurrentGuildId { get; set; }
        public ObservableCollection<EmbedViewModel> Embeds { get; set; }

        public MessageViewModel()
        {
        }

        // Helper to init wraps
        public void InitializeWrappers()
        {
            if (Message?.Embeds != null)
            {
                Embeds = new ObservableCollection<EmbedViewModel>(
                    Message.Embeds.Select(e => new EmbedViewModel { Embed = e, DiscordService = Service, CurrentGuildId = CurrentGuildId })
                );
            }
        }

        // We need to call InitializeWrappers after setting Service/Message
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public void RefreshContent()
        {
            _refreshId++;
            OnPropertyChanged(nameof(RefreshId));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(IsEdited));
            OnPropertyChanged(nameof(EditedLabelVisibility));

            if (Embeds != null)
            {
                foreach (var embed in Embeds)
                {
                    embed.Refresh();
                }
            }
        }

        private int _refreshId;
        public int RefreshId => _refreshId;

        public bool IsEdited => Message.EditedTimestamp.HasValue;
        public Visibility EditedLabelVisibility => IsEdited ? Visibility.Visible : Visibility.Collapsed;

        public bool ShowDateHeader { get; set; }
        public string DateHeaderText { get; set; }
        public Visibility DateHeaderVisibility => ShowDateHeader ? Visibility.Visible : Visibility.Collapsed;

        // Reactions
        public List<Reaction> Reactions => Message.Reactions;
        public Visibility ReactionsVisibility => (Message.HasReactions && !IsEditing) ? Visibility.Visible : Visibility.Collapsed;

        public void RefreshReactions()
        {
            OnPropertyChanged(nameof(Reactions));
            OnPropertyChanged(nameof(ReactionsVisibility));
        }

        // Replies
        public bool IsReply => Message.ReferencedMessage != null;
        public Visibility ReplyVisibility => IsReply ? Visibility.Visible : Visibility.Collapsed;

        public string ReplyAuthorName => Message.ReferencedMessage?.Author?.DisplayName ?? "Unknown User";
        public string ReplyAvatarUrl => Message.ReferencedMessage?.Author?.AvatarUrl ?? "https://cdn.discordapp.com/embed/avatars/0.png";
        
        public string ReplyContent 
        {
            get
            {
                if (Message.ReferencedMessage == null) return "Message deleted";
                string content = Message.ReferencedMessage.Content;
                if (string.IsNullOrEmpty(content))
                {
                    if (Message.ReferencedMessage.Attachments != null && Message.ReferencedMessage.Attachments.Count > 0)
                        return "Click to see attachment";
                    if (Message.ReferencedMessage.Embeds != null && Message.ReferencedMessage.Embeds.Count > 0)
                        return "Click to see embed";
                    return "Click to see message";
                }
                
                if (Message.ReferencedMessage.EditedTimestamp.HasValue)
                {
                    content += " (edited)";
                }
                
                // Truncate if too long (simple check, UI might handle it via TextTrimming)
                return content;
            }
        }
    }

    public class EmbedViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public NativeDiscord.Models.Embed Embed { get; set; }
        public DiscordService DiscordService { get; set; }
        public string CurrentGuildId { get; set; }

        private int _refreshId;
        public int RefreshId
        {
            get => _refreshId;
            set
            {
                _refreshId = value;
                OnPropertyChanged(nameof(RefreshId));
            }
        }

        public void Refresh()
        {
            RefreshId++;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => 
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
