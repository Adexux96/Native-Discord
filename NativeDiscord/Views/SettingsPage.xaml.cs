using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NativeDiscord.Services;

namespace NativeDiscord.Views
{
    public sealed partial class SettingsPage : Page
    {
        private DiscordService _discordService;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is DiscordService service)
            {
                _discordService = service;
                LogoutButton.IsEnabled = true;
                if (_discordService.CurrentUser != null)
                {
                    UserDisplayName.Text = _discordService.CurrentUser.DisplayName;
                    UserUsername.Text = _discordService.CurrentUser.Username;
                    UserAvatar.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(_discordService.CurrentUser.AvatarUrl));
                }
            }
        }

        private void LogoutButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_discordService == null) return;
            _discordService.Logout();
            if (App.MainWindow is MainWindow mw)
            {
                 mw.Logout();
            }
        }
    }
}
