using Microsoft.AspNetCore.Mvc;

using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MastodonController : ControllerBase
    {
        private readonly IMastodon _mastodon;

        public MastodonController(IMastodon mastodon)
        {
            _mastodon = mastodon;
        }

        private string GetRequestHost()
        {
            return string.Format($"{Request.Scheme}://{Request.Host.Value}");
        }

        [Route("auth")]
        [HttpGet]
        public async Task<ActionResult> AuthStart(string instance)
        {
            string redirectUrl;
            try
            {
                redirectUrl = await _mastodon.GetAuthenticationUrl(GetRequestHost(), instance);
            }
            catch (HttpRequestException hex)
            {
                return Content($"Sorry. Something went wrong. Most likely your instance wasn't correct: {hex.Message}");
            }
            catch (Exception ex)
            {
                return Content($"Sorry. Something went terribly wrong: {ex.Message}");
            }
            return new JsonResult(redirectUrl);
        }

        [Route("code")]
        [HttpGet]
        public async Task<ActionResult> AuthFinish(string instance, string code)
        {
            return new JsonResult(await _mastodon.UserIsAllowedToRegister(instance, code));
        }
    }
}
