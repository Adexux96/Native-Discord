using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NativeDiscord.Models;
using System;

namespace NativeDiscord.Controls
{
    public sealed partial class EmbedControl : UserControl
    {
        public static readonly DependencyProperty EmbedProperty =
            DependencyProperty.Register(nameof(Embed), typeof(Embed), typeof(EmbedControl), new PropertyMetadata(null, OnEmbedChanged));

        public Embed Embed
        {
            get => (Embed)GetValue(EmbedProperty);
            set => SetValue(EmbedProperty, value);
        }

        public EmbedControl()
        {
            this.InitializeComponent();
        }
        
        public static readonly DependencyProperty DiscordServiceProperty =
            DependencyProperty.Register(nameof(DiscordService), typeof(NativeDiscord.Services.DiscordService), typeof(EmbedControl), new PropertyMetadata(null));

        public NativeDiscord.Services.DiscordService DiscordService
        {
            get => (NativeDiscord.Services.DiscordService)GetValue(DiscordServiceProperty);
            set => SetValue(DiscordServiceProperty, value);
        }

        public static readonly DependencyProperty RefreshIdProperty =
            DependencyProperty.Register(nameof(RefreshId), typeof(int), typeof(EmbedControl), new PropertyMetadata(0, OnRefreshIdChanged));

        public int RefreshId
        {
            get => (int)GetValue(RefreshIdProperty);
            set => SetValue(RefreshIdProperty, value);
        }

        public static readonly DependencyProperty CurrentGuildIdProperty =
            DependencyProperty.Register(nameof(CurrentGuildId), typeof(string), typeof(EmbedControl), new PropertyMetadata(null));

        public string CurrentGuildId
        {
            get => (string)GetValue(CurrentGuildIdProperty);
            set => SetValue(CurrentGuildIdProperty, value);
        }

        private static void OnEmbedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmbedControl control)
            {
                control.Bindings.Update();
            }
        }

        private static void OnRefreshIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmbedControl control)
            {
                control.Bindings.Update();
            }
        }

        public Visibility TitleVisibility => !string.IsNullOrEmpty(Embed?.Title) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DescriptionVisibility => !string.IsNullOrEmpty(Embed?.Description) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ImageVisibility => !string.IsNullOrEmpty(Embed?.Image?.Url) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ThumbnailVisibility => !string.IsNullOrEmpty(Embed?.Thumbnail?.Url) ? Visibility.Visible : Visibility.Collapsed;

        public SolidColorBrush EmbedColorBrush
        {
            get
            {
                if (Embed != null && Embed.Color.HasValue)
                {
                    try
                    {
                        var c = Embed.Color.Value;
                        byte r = (byte)((c >> 16) & 0xFF);
                        byte g = (byte)((c >> 8) & 0xFF);
                        byte b = (byte)(c & 0xFF);
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
                    }
                    catch { }
                }
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 34, 37)); // Default dark
            }
        }
    }
}
