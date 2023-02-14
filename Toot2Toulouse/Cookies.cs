using Mastonet.Entities;

using System.Text.Json;

using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class Cookies : ICookies
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CookieOptions _cookieOptions;

        public Cookies(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _cookieOptions = new CookieOptions
            {
                Secure = true,
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMonths(6)
            };
        }

        public void AppRegistrationSetSession(AppRegistration appRegistration)
        {
            SetSessionValue(nameof(AppRegistration), appRegistration);
        }

        public AppRegistration AppRegistrationGetSession()
        {
            return GetSessionValue<AppRegistration>(nameof(AppRegistration));
        }

        public CookiePair GetUserCookie()
        {
            return new CookiePair
            {
                Hash = GetCookieValue<string>("hash"),
                Userid = GetCookieValue<Guid>("id")
            };
        }

        public void SetUserCookie(CookiePair cookiePair)
        {
            SetCookieValue("id", cookiePair.Userid);
            SetCookieValue("hash", cookiePair.Hash);
        }

        private void SetSessionValue<T>(string name, T value)
        {
            var context = _httpContextAccessor.HttpContext;
            context.Session.SetString(name, JsonSerializer.Serialize(value, ConfigReader.JsonOptions));
        }

        private T GetSessionValue<T>(string name)
        {
            var context = _httpContextAccessor.HttpContext;
            return JsonSerializer.Deserialize<T>(context.Session.GetString(name), ConfigReader.JsonOptions);
        }

        private void SetCookieValue<T>(string name, T value)
        {
            var context = _httpContextAccessor.HttpContext;
            context.Response.Cookies.Append(name, JsonSerializer.Serialize(value, ConfigReader.JsonOptions), _cookieOptions);
        }

        private T GetCookieValue<T>(string name)
        {
            var context = _httpContextAccessor.HttpContext;
            var cookieValue = context.Request.Cookies[name];
            if (cookieValue == null) return default(T);
            return JsonSerializer.Deserialize<T>(cookieValue, ConfigReader.JsonOptions);
        }
    }
}