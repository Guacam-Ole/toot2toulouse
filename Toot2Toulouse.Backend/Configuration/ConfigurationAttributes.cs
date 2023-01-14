namespace Toot2Toulouse.Backend.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OverviewCategory : Attribute
    {
        public string Category { get; set; }
        public string? DisplayName { get; set; }
        public string? NullText { get; set; }
        public string? Suffix { get; set; }

        public OverviewCategory(string category, string? displayName = null)
        {
            Category = category;
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class HideOnExport: Attribute
    {
    }
}