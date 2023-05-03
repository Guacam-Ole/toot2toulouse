using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Toot2ToulouseWeb;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    //[Route("/")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AppController : ControllerBase
    {
        private readonly IToulouse _app;
        private readonly ILogger<AppController> _logger;
        private TootConfiguration _config;

        public AppController(ILogger<AppController> logger, ConfigReader configReader, IToulouse app, IMastodon mastodon)
        {
            _logger = logger;
            _logger.LogDebug("Reading config");
            _config = configReader.Configuration;
            _logger.LogDebug("Config read");
            _app = app;
        }

        [Route("/")]
        public ActionResult Index()
        {
            return new RedirectResult($"index.{_config.App.Languages.Default}.html");
        }

        [Route("server")]
        public ActionResult GetServerSettings()
        {
            return JsonResults.Success(_app.GetServerSettingsForDisplay());
        }

        [Route("config")]
        public ActionResult GetServerSettingsStructured()
        {
            return JsonResults.Success(_config.App);
        }

        [Route("stats")]
        public async Task<ActionResult> GetServerStats()
        {
            return JsonResults.Success(await _app.CalculateServerStats());
        }

        [Route("autherror")]
        public ActionResult Autherror()
        {
            return new RedirectResult($"auth.{_config.App.Languages.Default}.html");
        }

        [Route("error")]
        public ActionResult Error(string code)
        {
            return new RedirectResult($"error.{_config.App.Languages.Default}.html?error={code}");
        }

        [Route("ping")]
        [HttpPost]
        public ActionResult Ping([FromBody] string pingdata)
        {
            // ToDo: Store
            var pingObject = JsonConvert.DeserializeObject<PingData>(pingdata);
            return JsonResults.Success();
        }

        [Route("pingget")]
        public ActionResult PingGet()
        {
            // ToDo: Store
            return JsonResults.Success();
        }
    }
}