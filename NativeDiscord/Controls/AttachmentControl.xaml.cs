using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NativeDiscord.Models;

namespace NativeDiscord.Controls
{
    public sealed partial class AttachmentControl : UserControl
    {
        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register(nameof(Attachment), typeof(Attachment), typeof(AttachmentControl), new PropertyMetadata(null, OnAttachmentChanged));

        public Attachment Attachment
        {
            get => (Attachment)GetValue(AttachmentProperty);
            set => SetValue(AttachmentProperty, value);
        }

        public AttachmentControl()
        {
            this.InitializeComponent();
        }

        private static void OnAttachmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AttachmentControl control)
            {
                control.Bindings.Update();
            }
        }
        
        public Visibility ImageVisibility => (Attachment?.IsImage == true) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FileVisibility => (Attachment?.IsImage == false) ? Visibility.Visible : Visibility.Collapsed;
    }
}
