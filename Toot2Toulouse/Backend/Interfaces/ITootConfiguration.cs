using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITootConfiguration
    {
        public Secrets GetSecrets();
    }
}
