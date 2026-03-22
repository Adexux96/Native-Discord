using System.Text.Json.Serialization;

namespace NativeDiscord.Models
{
    public class Activity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; } // 0=Game, 1=Streaming, 2=Listening, 3=Watching, 4=Custom, 5=Competing

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("assets")]
        public ActivityAssets Assets { get; set; }

        [JsonPropertyName("timestamps")]
        public ActivityTimestamps Timestamps { get; set; }

        // Mapped from external fetch
        public string ResolvedApplicationIconUrl { get; set; }

        // --- Helpers for Asset Resolution ---

        public string HeaderIconUrl
        {
            get
            {
                // Logic:
                // 1. If has SmallImage, use it
                // 2. Else if has LargeImage, use it
                // 3. If we resolved an Application Icon (e.g. Roblox), use it!
                // 4. Fallback to generic icon based on Type

                string url = GetImageUrl(Assets?.SmallImage);
                if (url == null) url = GetImageUrl(Assets?.LargeImage);

                if (url != null) return url;

                // Use resolved app icon if we found one
                if (!string.IsNullOrEmpty(ResolvedApplicationIconUrl))
                    return ResolvedApplicationIconUrl;

                // Fallbacks (Local Assets)
                switch(Type)
                {
                    case 0: return "ms-appx:///Assets/generic_game_icon.png";
                    case 1: return "ms-appx:///Assets/generic_music_icon.png"; // Streaming
                    case 2: return "ms-appx:///Assets/generic_music_icon.png"; // Listening
                    default: return "ms-appx:///Assets/generic_game_icon.png";
                }
            }
        }

        public string PrimaryImageUrl
        {
            get
            {
                return GetImageUrl(Assets?.LargeImage);
            }
        }

        public string SecondaryImageUrl => GetImageUrl(Assets?.SmallImage);

        public bool HasPrimaryImage => !string.IsNullOrEmpty(PrimaryImageUrl);
        public bool HasSecondaryImage => !string.IsNullOrEmpty(SecondaryImageUrl);

        // Only show the big rich presence card if we actually have rich data (Images, Details, or specific State)
        public bool IsRichPresence => (Assets != null && (HasPrimaryImage || HasSecondaryImage)) || !string.IsNullOrEmpty(Details) || !string.IsNullOrEmpty(State);

        private string GetImageUrl(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;

            // 1. Spotify
            if (assetId.StartsWith("spotify:"))
            {
                return $"https://i.scdn.co/image/{assetId.Substring("spotify:".Length)}";
            }

            // 2. External
            if (assetId.StartsWith("mp:external/"))
            {
                return assetId.Replace("mp:", "https://media.discordapp.net/");
            }

            // 3. Standard Discord CDN Asset
            if (!string.IsNullOrEmpty(ApplicationId))
            {
                return $"https://cdn.discordapp.com/app-assets/{ApplicationId}/{assetId}.png";
            }

            return null;
        }
    }

    public class ActivityTimestamps
    {
        [JsonPropertyName("start")]
        public long? Start { get; set; }

        [JsonPropertyName("end")]
        public long? End { get; set; }
    }

    public class ActivityAssets
    {
        [JsonPropertyName("large_image")]
        public string LargeImage { get; set; }

        [JsonPropertyName("large_text")]
        public string LargeText { get; set; }

        [JsonPropertyName("small_image")]
        public string SmallImage { get; set; }

        [JsonPropertyName("small_text")]
        public string SmallText { get; set; }
    }

    public class Application
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }
}
