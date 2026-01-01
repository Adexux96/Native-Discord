using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace NativeDiscord.Views
{
    /// <summary>
    /// Static helper functions for safe XAML bindings
    /// </summary>
    public static class Converters
    {
        /// <summary>
        /// Safely converts a URL string to an ImageSource.
        /// Returns null if the URL is null, empty, or invalid - preventing crashes.
        /// </summary>
        public static ImageSource ToImageSource(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                return new BitmapImage(new Uri(url, UriKind.Absolute));
            }
            catch
            {
                // Return null for any malformed URLs - the Image control will just show nothing
                return null;
            }
        }

        /// <summary>
        /// Safely converts a URL string to an ImageSource with a fallback.
        /// </summary>
        public static ImageSource ToImageSourceWithFallback(string url, string fallbackUrl)
        {
            var result = ToImageSource(url);
            if (result == null && !string.IsNullOrEmpty(fallbackUrl))
            {
                result = ToImageSource(fallbackUrl);
            }
            return result;
        }
    }

    public class BooleanToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
                return Microsoft.UI.Xaml.Visibility.Visible;
            return Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Microsoft.UI.Xaml.Visibility v && v == Microsoft.UI.Xaml.Visibility.Visible;
        }
    }
}
