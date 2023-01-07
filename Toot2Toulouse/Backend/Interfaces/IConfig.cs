using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IConfig
    {
        public Backend.Configuration.TootConfiguration GetConfig();
    }
}
