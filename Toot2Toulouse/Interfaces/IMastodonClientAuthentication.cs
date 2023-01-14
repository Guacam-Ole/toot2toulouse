namespace Toot2Toulouse.Interfaces
{
    public interface IMastodonClientAuthentication
    {
        Task<string> GetAuthenticationUrl(string requestHost, string userInstance);

        Task<KeyValuePair<bool, string>> UserIsAllowedToRegister(string userInstance, string verificationCode);
    }
}
