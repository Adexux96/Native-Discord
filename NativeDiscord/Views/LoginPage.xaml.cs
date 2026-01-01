using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace NativeDiscord.Views
{
    public sealed partial class LoginPage : Page
    {
        public event EventHandler<string> TokenReceived;

        public LoginPage()
        {
            this.InitializeComponent();
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            await LoginWebView.EnsureCoreWebView2Async();
            
            // Intercept headers to find the token
            LoginWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            LoginWebView.CoreWebView2.AddWebResourceRequestedFilter("https://discord.com/api/*", CoreWebView2WebResourceContext.All);
        }

        private void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            // Check for Authorization header in outgoing API requests
            if (args.Request.Headers.Contains("Authorization"))
            {
                string token = args.Request.Headers.GetHeader("Authorization");
                
                // We typically want the user token, which is not usually "Bot ..."
                // Simple validation: Ensure it's not null/empty
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Execute on UI thread to update UI or fire events
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // Avoid firing multiple times
                        LoginWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                        ShowLoading();
                        TokenReceived?.Invoke(this, token);
                    });
                }
            }
        }

        private void ShowLoading()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoginWebView.Visibility = Visibility.Collapsed;
        }
        
        // Public method to clean up resources explicitly if needed
        public void Cleanup()
        {
            if (LoginWebView.CoreWebView2 != null)
            {
                LoginWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
            }
            // LoginWebView.Close(); // POTENTIAL CRASH SOURCE: Closing WebView2 while app is transitioning frames might cause AV.
            // Just letting it be destroyed by Garbage/Visual Tree removal is safer.
        }
    }
}
