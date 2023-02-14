using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Interfaces;

using Toot2ToulouseWeb;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class MastodonController : ControllerBase
    {
        private readonly IMastodonClientAuthentication _mastodon;

        public MastodonController(IMastodonClientAuthentication mastodon)
        {
            _mastodon = mastodon;
        }

        private string GetRequestHost()
        {
            return string.Format($"{Request.Scheme}://{Request.Host.Value}");
        }

        [Route("auth")]
        [HttpGet]
        public async Task<ActionResult> AuthStartAsync(string instance)
        {
            string redirectUrl;
            try
            {
                redirectUrl = await _mastodon.GetAuthenticationUrlAsync(GetRequestHost(), instance);
            }
            catch (HttpRequestException hex)
            {
                throw new ApiException(ApiException.ErrorTypes.Mastodon, $"Sorry. Something went wrong. Most likely your instance wasn't correct: {hex.Message}");
            }
            catch (Exception ex)
            {
                throw new ApiException(ApiException.ErrorTypes.Exception, $"Sorry. Something went terribly wrong: {ex.Message}");
            }
            return JsonResults.Success(redirectUrl);
        }

        [Route("code")]
        [HttpGet]
        public async Task<ActionResult> AuthFinishAsync(string instance, string code)
        {
            await _mastodon.UserIsAllowedToRegisterAsync(instance, code);
            return JsonResults.Success();
        }
    }
}