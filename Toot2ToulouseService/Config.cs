using Microsoft.Extensions.Logging;

namespace Toot2ToulouseService
{
    public class Config
    {
        public Paths Paths { get; set; }
        public string LogLevel { get; set; }  
    }
}