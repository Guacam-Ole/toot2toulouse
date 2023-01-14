using Mastonet.Entities;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ICookies
    {
        void AppRegistrationSetSession(AppRegistration appRegistration);
        AppRegistration AppRegistrationGetSession();

        Guid UserIdGetCookie();
        string UserHashGetCookie();

        void UserIdSetCookie(Guid userId);
        void UserHashSetCookie(string hash);

    }
}
