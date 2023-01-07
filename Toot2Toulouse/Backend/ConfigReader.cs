using Newtonsoft.Json;

using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend
{
    public class ConfigReader
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public TootConfiguration Configuration { get; set; }

        public ConfigReader(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            Configuration = GetConfig();
        }

        private TootConfiguration GetConfig()
        {
            return ReadJsonFile<TootConfiguration>("config.json");
        }

        public T ReadJsonFile<T>(string filename)
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