using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using NativeDiscord.Models;
using System;

namespace NativeDiscord.Controls
{
    public sealed partial class ReactionControl : UserControl
    {
        public static readonly DependencyProperty ReactionProperty = DependencyProperty.Register(
            nameof(Reaction),
            typeof(Reaction),
            typeof(ReactionControl),
            new PropertyMetadata(null, OnReactionChanged));

        public Reaction Reaction
        {
            get => (Reaction)GetValue(ReactionProperty);
            set => SetValue(ReactionProperty, value);
        }

        private static void OnReactionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReactionControl control)
            {
                if (e.OldValue is NativeDiscord.Models.Reaction oldR)
                {
                     oldR.PropertyChanged -= control.OnReactionPropertyChanged;
                }
                if (e.NewValue is NativeDiscord.Models.Reaction newR)
                {
                     newR.PropertyChanged += control.OnReactionPropertyChanged;
                }
                control.Render();
            }
        }

        private void OnReactionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() => Render());
        }

        public event EventHandler<Reaction> ReactionClicked;

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Reaction != null)
            {
                ReactionClicked?.Invoke(this, Reaction);
            }
        }

        public ReactionControl()
        {
            this.InitializeComponent();
        }

        private void Render()
        {
            if (Reaction == null) return;

            CountText.Text = Reaction.Count.ToString();

            // Handle styling for "Me" (if I reacted)
            if (Reaction.Me)
            {
                (this.Content as Grid).Background = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 88, 101, 242));
                (this.Content as Grid).BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 88, 101, 242)); 
                CountText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
            else
            {
                 // Default Styling
                 (this.Content as Grid).Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 47, 49, 54)); // #2F3136
                 (this.Content as Grid).BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                 CountText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 185, 187, 190)); // #B9BBBE
            }

            if (Reaction.Emoji.IsCustom)
            {
                EmojiText.Visibility = Visibility.Collapsed;
                EmojiImage.Visibility = Visibility.Visible;
                
                if (!string.IsNullOrEmpty(Reaction.Emoji.Url))
                {
                    EmojiImage.Source = new BitmapImage(new Uri(Reaction.Emoji.Url));
                }
            }
            else
            {
                EmojiImage.Visibility = Visibility.Collapsed;
                EmojiText.Visibility = Visibility.Visible;
                EmojiText.Text = Reaction.Emoji.Name; // Contains the unicode char
            }
            
            // Tooltip
            ToolTipService.SetToolTip(this, $"{Reaction.Emoji.Name}");
        }
    }
}
