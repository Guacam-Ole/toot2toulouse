namespace Toot2Toulouse.Interfaces
{
    public interface IMastodonClientAuthentication
    {
        Task<string> GetAuthenticationUrlAsync(string requestHost, string userInstance);

        Task<KeyValuePair<bool, string>> UserIsAllowedToRegisterAsync(string userInstance, string verificationCode);
    }
}
