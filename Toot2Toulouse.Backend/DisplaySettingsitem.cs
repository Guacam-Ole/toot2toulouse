using System.Runtime;

namespace Toot2Toulouse.Backend
{
    public class DisplaySettingsItem
    {
        public string Category { get; set; }    
        public string Path { get; set; }    
        public string DisplayName { get; set; }
        public object? Value { get; set; }
        public bool DisplayAsButton { get; set; }
    }
}
