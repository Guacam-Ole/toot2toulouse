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
        public async Task<ActionResult> Register()
        {
            var serverMode = await _app.GetServerMode();
            if (serverMode == TootConfigurationAppModes.ValidModes.Closed) return new RedirectResult($"closed.{_config.App.Languages.Default}.html");
            return new RedirectResult($"/register.{_config.App.Languages.Default}.html");
        }

        [Route("export")]
        public async Task<ActionResult> GetUserExport()
        {
            var user = await GetUserFromCookie();
            return new JsonResult(_user.ExportUserData(user));
        }

        private async Task<UserData> GetUserFromCookie(bool refresh = false)
        {
            var userCookie = _cookies.GetUserCookie();

            if (userCookie.Userid == Guid.Empty || userCookie.Hash == null) throw new ApiException(ApiException.ErrorTypes.Auth);
            var user = await _user.GetUser(userCookie.Userid, userCookie.Hash);
            if (user == null) throw new ApiException(ApiException.ErrorTypes.Auth);
            if (refresh) _cookies.SetUserCookie(userCookie);
            return user;
        }

        [Route("visibility")]
        public async Task<ActionResult> UpdateConfigVisibility(bool publicToots, bool notListedToots, bool privateToots)
        {
            var user = await GetUserFromCookie();
            user.Config.VisibilitiesToPost = new List<UserConfiguration.Visibilities>();
            if (publicToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Public);
            if (notListedToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Unlisted);
            if (privateToots) user.Config.VisibilitiesToPost.Add(UserConfiguration.Visibilities.Private);
            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("donttweet")]
        [HttpPost]
        public async Task<ActionResult> UpdateDontTweet([FromBody] List<string> badwords)
        {
            var user = await GetUserFromCookie();
            user.Config.DontTweet = badwords.Where(q => !string.IsNullOrWhiteSpace(q)).Distinct().ToList();
            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("translations")]
        [HttpPost]
        public async Task<ActionResult> AddTranslation([FromBody] List<KeyValuePair<string, string>> translations)
        {
            var user = await GetUserFromCookie();
            user.Config.Replacements = new Dictionary<string, string>();

            foreach (var pair in translations)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || user.Config.Replacements.ContainsKey(pair.Key)) continue;
                user.Config.Replacements.Add(pair.Key, pair.Value);
            }
            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("delay")]
        public async Task<ActionResult> UpdateConfigDelay(TimeSpan delay)
        {
            var user = await GetUserFromCookie();
            user.Config.Delay = delay.SetLimits(_config.App.Intervals.MinDelay, _config.App.Intervals.MaxDelay);
            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("suffix")]
        public async Task<ActionResult> UpdateConfigAppSuffix(string content, bool hideIfBreaks)
        {
            var user = await GetUserFromCookie();
            user.Config.AppSuffix.Content = content.Shorten(10);
            user.Config.AppSuffix.HideIfBreaks = hideIfBreaks;

            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("thread")]
        public async Task<ActionResult> UpdateConfigLongContent(string prefix, string suffix)
        {
            var user = await GetUserFromCookie();
            user.Config.LongContentThreadOptions.Prefix = prefix.Shorten(10);
            user.Config.LongContentThreadOptions.Suffix = suffix.Shorten(10);

            await _user.UpdateUser(user);
            return JsonResults.Success();
        }

        [Route("config")]
        public ActionResult Config()
        {
            return new RedirectResult($"/config.{_config.App.Languages.Default}.html");
        }

        [Route("lists")]
        public ActionResult Lists()
        {
            return new RedirectResult($"/lists.{_config.App.Languages.Default}.html");
        }
    }
}