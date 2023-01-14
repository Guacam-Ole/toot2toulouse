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

        [Route("")]
        public ActionResult Index()
        {
            return new RedirectResult($"./Frontend/index.{_config.App.DefaultLanguage}.html");
        }

        [Route("register")]
        public ActionResult Register()
        {
            if (_config.App.Modes.Active == TootConfigurationAppModes.ValidModes.Closed) return new RedirectResult($"../Frontend/closed.{_config.App.DefaultLanguage}.html");
            return new RedirectResult($"../Frontend/register.{_config.App.DefaultLanguage}.html");
        }

        [Route("server")]
        public ActionResult GetServerSettings()
        {
            return new JsonResult(_app.GetServerSettingsForDisplay());
        }

        [Route("export")]
        public async Task<ActionResult> GetUserExport()
        {
            var id = _cookies.UserIdGetCookie();
            var hash = _cookies.UserHashGetCookie();
            var user = _user.GetUser(id, hash);
            return new JsonResult(_user.ExportUserData(user));
        }
    }
}