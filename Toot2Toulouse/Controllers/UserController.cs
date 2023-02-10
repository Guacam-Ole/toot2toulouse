using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

namespace Toot2ToulouseWeb.Controllers
{
    [ApiController]
    [Route("user")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IToulouse _app;
        private readonly IMastodon _mastodon;
        private readonly IUser _user;
        private readonly ICookies _cookies;
        private readonly TootConfiguration _config;

        public UserController(ILogger<UserController> logger, ConfigReader configReader, IToulouse app, IMastodon mastodon, IUser user, ICookies cookies)
        {
            _logger = logger;
            _app = app;
            _mastodon = mastodon;
            _user = user;
            _cookies = cookies;
            _config = configReader.Configuration;
        }

        [Route("register")]
        public ActionResult Register()
        {
            var serverMode = _app.GetServerMode();
            if (serverMode == TootConfigurationAppModes.ValidModes.Closed) return new RedirectResult($"closed.{_config.App.DefaultLanguage}.html");
            return new RedirectResult($"/register.{_config.App.DefaultLanguage}.html");
        }

        [Route("export")]
        public ActionResult GetUserExport() 
        {
            var user = GetUserFromCookie();
            if (user == null) return AuthErrorResult();
            return new JsonResult(_user.ExportUserData(user));
        }

        private UserData? GetUserFromCookie()
        {
            var id = _cookies.UserIdGetCookie();
            var hash = _cookies.UserHashGetCookie();
            if (id == Guid.Empty || hash == null) return null;
            return _user.GetUser(id, hash);
        }

        private JsonResult AuthErrorResult()
        {
            return new JsonResult(new { Error = "auth", Success = false });
        }

        private JsonResult SuccessResult()
        {
            return new JsonResult(new { Success = true });
        }

        [Route("visibility")]
        public ActionResult UpdateConfigVisibility(bool publicToots, bool notListedToots, bool privateToots)
        {
            var user = GetUserFromCookie();
            if (user == null) return AuthErrorResult();
            user.Config.VisibilitiesToPost = new List<UserConfiguration.Visibilities>();
            if (publicToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Public);
            if (notListedToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Unlisted);
            if (privateToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Private);
            _user.UpdateUser(user);
            return SuccessResult();
        }

        [Route("delay")]
        public ActionResult UpdateConfigDelay(TimeSpan delay)
        {
            var user = GetUserFromCookie();
            if (user == null) return AuthErrorResult();
            user.Config.Delay = delay.SetLimits(_config.App.Intervals.MinDelay, _config.App.Intervals.MaxDelay);
            _user.UpdateUser(user);
            return SuccessResult();
        }

        [Route("suffix")]
        public ActionResult UpdateConfigAppSuffix(string content, bool hideIfBreaks)
        {
            var user = GetUserFromCookie();
            if (user == null) return AuthErrorResult();
            user.Config.AppSuffix.Content = content.Shorten(10);
            user.Config.AppSuffix.HideIfBreaks = hideIfBreaks;

            _user.UpdateUser(user);
            return SuccessResult();
        }

        [Route("thread")]
        public ActionResult UpdateConfigLongContent(string prefix, string suffix)
        {
            var user = GetUserFromCookie();
            if (user == null) return AuthErrorResult();
            user.Config.LongContentThreadOptions.Prefix = prefix.Shorten(10);
            user.Config.LongContentThreadOptions.Suffix = suffix.Shorten(10);

            _user.UpdateUser(user);
            return SuccessResult();
        }

        [Route("config")]
        public ActionResult Config()
        {
            return new RedirectResult($"/config.{_config.App.DefaultLanguage}.html");
        }
    }
}