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
        private readonly Mastodon _mastodon;
        private TootConfiguration _config;

        public AppController(ILogger<TwitterAuthController> logger, ConfigReader configReader, App app, Mastodon mastodon)
        {
            _config = configReader.Configuration;
            _app = app;
            _mastodon = mastodon;
        }


        [Route("test")]
        public async Task<ActionResult> TestToot()
        {
            await _mastodon.SendAllStatusMessagesTo("@stammtischphilosoph@chaos.social");
            //await _app.ServiceToot("Das ist ein private test an @stammtischphilosoph@chaos.social. How awesome is that?🎉🥂💀⚽", Mastonet.Visibility.Direct);
            return null; // new JsonResult();
        }

        [HttpGet,Route("create")]
        public async Task<ActionResult> Create()
        {
            var newConfig=await _mastodon.CreateNewApp(_config.App, _config.Secrets.Mastodon);
            return new JsonResult(newConfig);
        }

        [Route("latest")]
        public async Task<ActionResult> GetLatestTootFromApp()
        {
            await _app.TweetServicePosts();
            //await _app.ServiceToot("Das ist ein private test an @stammtischphilosoph@chaos.social. How awesome is that?🎉🥂💀⚽", Mastonet.Visibility.Direct);
            return null; // new JsonResult();
        }


    }
}
