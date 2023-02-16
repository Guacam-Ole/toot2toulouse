namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationAppTwitterLimits
    {
        [OverviewCategory("Twitter Attachments", "Maximum Filesize for static images", Suffix = " MB")]
        public int MaxImageSize { get; set; }

        [OverviewCategory("Twitter Attachments", "Maximum Filesize for GIFs", Suffix = " MB")]
        public int MaxGifSize { get; set; }

        [OverviewCategory("Twitter Attachments", "Maximum Filesize for embedded Videos", Suffix = " MB")]
        public int MaxVideoSize { get; set; }

        public int CharacterLimit { get; set; }
        public int MinSplitLength { get; set; } // when trying to split long toots by space this is number of characters the algorithm gives up and splits inside a word
    }
}