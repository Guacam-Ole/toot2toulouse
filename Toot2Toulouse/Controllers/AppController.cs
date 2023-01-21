using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("/")]
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

        [Route("register")]
        public ActionResult Register()
        {
            if (_config.App.Modes.Active == TootConfigurationAppModes.ValidModes.Closed) return new RedirectResult($"closed.{_config.App.DefaultLanguage}.html");
            return new RedirectResult($"register.{_config.App.DefaultLanguage}.html");
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


        [Route("config")]
        public ActionResult Config()
        {
            return new RedirectResult($"config.{_config.App.DefaultLanguage}.html");
        }

        [Route("export")]
        public async Task<ActionResult> GetUserExport()
        {
            var id = _cookies.UserIdGetCookie();
            var hash = _cookies.UserHashGetCookie();
            if (id == Guid.Empty || hash == null) return AuthErrorResult();
            var user = _user.GetUser(id, hash);
            return new JsonResult(_user.ExportUserData(user));
        }

        [Route("autherror")]
        public ActionResult Autherror()
        {
            return new RedirectResult($"unknown.{_config.App.DefaultLanguage}.html");
        }

        private JsonResult AuthErrorResult()
        {
            return new JsonResult(new { Error = "auth", Success = false });
        }
    }
}