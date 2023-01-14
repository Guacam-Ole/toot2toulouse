namespace Toot2Toulouse.Interfaces
{
    public interface ITwitterClientAuthentication
    {
        Task<bool> FinishAuthenticationAsync(string query);

        Task<string> GetAuthenticationUrlAsync(string baseUrl);
    }
}
