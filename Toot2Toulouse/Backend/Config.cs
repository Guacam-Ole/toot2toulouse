using Newtonsoft.Json;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class Config : IConfig
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public Config(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;

        }
        public TootConfiguration GetConfig()
        {
            return ReadJsonConfig<TootConfiguration>("config.json");
        }

        private T ReadJsonConfig<T>(string filename)
        {
            string fullpath = Path.Combine(_webHostEnvironment.ContentRootPath, "Properties", filename);
            using (StreamReader r = new StreamReader(fullpath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}