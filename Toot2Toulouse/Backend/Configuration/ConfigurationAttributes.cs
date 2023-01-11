namespace Toot2Toulouse.Backend.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ShowOnOverview : Attribute
    {
        public bool Show { get; set; }

        public ShowOnOverview(bool show = true)
        { Show = show; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class OverviewCategory : Attribute
    {
        public string Category { get; set; }

        public OverviewCategory(string category)
        {
            Category = category;
        }
    }
}