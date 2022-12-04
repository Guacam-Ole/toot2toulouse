using Newtonsoft.Json;

using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class TootConfiguration : ITootConfiguration
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TootConfiguration(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;

        }
        public Secrets GetSecrets()
        {
            return ReadJsonConfig<Secrets>("secrets.json");
        }

        private T ReadJsonConfig<T>(string filename)
        {
            string fullpath = Path.Combine(_webHostEnvironment.ContentRootPath, "Properties", filename);
            // PlatformServices.ApplicationEnvironment.
            using (StreamReader r = new StreamReader(fullpath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
