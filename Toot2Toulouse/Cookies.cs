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
                Path = "/"
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

        public Guid UserIdGetCookie()
        {
            return GetCookieValue<Guid>("id");
        }

        public string UserHashGetCookie()
        {
            return GetCookieValue<string>("hash");
        }

        public void UserIdSetCookie(Guid userId)
        {
            SetCookieValue("id", userId);
        }

        public void UserHashSetCookie(string hash)
        {
            SetCookieValue("hash", hash);
        }

        private void SetSessionValue<T>(string name, T value)
        {
            var context = _httpContextAccessor.HttpContext;
            context.Session.SetString(name, JsonSerializer.Serialize(value));
        }

        private T GetSessionValue<T>(string name)
        {
            var context = _httpContextAccessor.HttpContext;
            return JsonSerializer.Deserialize<T>(context.Session.GetString(name));
        }

        private void SetCookieValue<T>(string name, T value)
        {
            var context = _httpContextAccessor.HttpContext;
            context.Response.Cookies.Append(name, JsonSerializer.Serialize(value), _cookieOptions);
        }

        private T GetCookieValue<T>(string name)
        {
            var context = _httpContextAccessor.HttpContext;
            var cookieValue = context.Request.Cookies[name];
            if (cookieValue == null) return default(T);
            return JsonSerializer.Deserialize<T>(cookieValue);
        }
    }
}