using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppController : ControllerBase
    {
        private readonly App _app;
        private TootConfiguration _config;

        public AppController(ILogger<TwitterAuthController> logger, Backend.Interfaces.IConfig configuration, App app)
        {
            _config=configuration.GetConfig();
            _app = app;
        }


        [Route("test")]
        public async Task<ActionResult> TestToot()
        {
            await _app.SendAllStatusMessagesTo("@stammtischphilosoph@chaos.social");
            //await _app.ServiceToot("Das ist ein private test an @stammtischphilosoph@chaos.social. How awesome is that?🎉🥂💀⚽", Mastonet.Visibility.Direct);
            return null; // new JsonResult();
        }

        [HttpGet,Route("create")]
        public async Task<ActionResult> Create()
        {
            var newConfig=await _app.CreateNewApp(_config.App, _config.Secrets.Mastodon);
            return new JsonResult(newConfig);
        }
    }
}
