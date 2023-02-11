namespace Toot2Toulouse.Interfaces
{
    public interface IMastodonClientAuthentication
    {
        Task<string> GetAuthenticationUrlAsync(string requestHost, string userInstance);

        Task UserIsAllowedToRegisterAsync(string userInstance, string verificationCode);
    }
}
