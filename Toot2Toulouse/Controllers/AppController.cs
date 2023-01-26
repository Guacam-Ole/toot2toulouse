using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("/")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AppController : ControllerBase
    {
        private readonly IToulouse _app;
        private readonly IUser _user;
        private readonly ICookies _cookies;
        private TootConfiguration _config;

        public AppController(ILogger<TwitterController> logger, ConfigReader configReader, IToulouse app, IMastodon mastodon, IUser user, ICookies cookies)
        {
            _config = configReader.Configuration;
            _app = app;
            _user = user;
            _cookies = cookies;
        }

        [Route("/")]
        public ActionResult Index()
        {
            return new RedirectResult($"index.{_config.App.DefaultLanguage}.html");
        }

     

        [Route("server")]
        public ActionResult GetServerSettings()
        {
            return new JsonResult(_app.GetServerSettingsForDisplay());
        }

        [Route("disclaimer")]
        public ActionResult GetDisclaimer()
        {
            return new JsonResult(_config.App.Disclaimer);
        }

        [Route("stats")]
        public ActionResult GetServerStats()
        {
            return null;
        }


   

     

        [Route("autherror")]
        public ActionResult Autherror()
        {
            return new RedirectResult($"unknown.{_config.App.DefaultLanguage}.html");
        }

       
    }
}