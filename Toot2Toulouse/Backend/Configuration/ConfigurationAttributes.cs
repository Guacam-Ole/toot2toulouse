namespace Toot2Toulouse.Backend.Configuration
{
    //[AttributeUsage(AttributeTargets.Property)]
    //public class ShowOnOverview : Attribute
    //{
    //    public bool Show { get; set; }
    //    public string? Displayname { get; set; }

    //    public ShowOnOverview(string? displayname = null, bool show = true)
    //    {
    //        Show = show;
    //        Displayname = displayname;
    //    }
    //}

    [AttributeUsage(AttributeTargets.Property)]
    public class OverviewCategory : Attribute
    {
        public string Category { get; set; }
        public string? DisplayName { get; }

        public OverviewCategory(string category, string? displayName=null)
        {
            Category = category;
            DisplayName = displayName;
        }
    }
}