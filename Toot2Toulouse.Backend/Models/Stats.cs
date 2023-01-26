
using Microsoft.Extensions.Logging;

namespace Toot2Toulouse.Backend.Models
{
    public class Stats
    {
        public Guid Id { get; set; }=Guid.NewGuid();    
        public string CurrentVersion { get; set; }
        public long ActiveUsers { get; set; }
        public long TotalUsers { get; set; }    
    }
}