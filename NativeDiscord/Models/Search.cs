namespace NativeDiscord.Models
{
    public class SearchResultItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string IconUrl { get; set; }
        public string Glyph { get; set; } // For font icons
        public string Type { get; set; } // "Channel", "User", "Server"
        public object OriginalObject { get; set; } // The Channel, User, or Server object

        public bool ShowImage => !string.IsNullOrEmpty(IconUrl);
        public bool ShowGlyph => !string.IsNullOrEmpty(Glyph);
    }
}
