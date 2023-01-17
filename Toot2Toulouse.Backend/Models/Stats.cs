using Newtonsoft.Json.Serialization;

namespace Toot2Toulouse.Backend.Models
{
    public class Stats
    {
        public Guid Id { get; set; }=Guid.NewGuid();    
        public string CurrentVersion { get; set; }
    }
}