using Mastonet.Entities;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ICookies
    {
        void AppRegistrationSetSession(AppRegistration appRegistration);

        AppRegistration AppRegistrationGetSession();

        CookiePair GetUserCookie();

        void SetUserCookie(CookiePair cookiePair);
    }

    public class CookiePair
    {
        public Guid Userid { get; set; }
        public string Hash { get; set; }
    }
}