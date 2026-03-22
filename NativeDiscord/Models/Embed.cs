using System;
using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Embed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("color")]
        public int? Color { get; set; }

        [JsonPropertyName("image")]
        public EmbedImage Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public EmbedImage Thumbnail { get; set; }

        // Helper to convert int Color to Hex
        public string ColorHex
        {
            get
            {
                if (Color.HasValue)
                {
                    // Standard int color is typically ARGB or RGB? Discord uses integer representation of RGB.
                    return string.Format("#{0:X6}", Color.Value & 0xFFFFFF);
                }
                return "#202225"; // Default embed border or background
            }
        }
    }

    public class EmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }
}
