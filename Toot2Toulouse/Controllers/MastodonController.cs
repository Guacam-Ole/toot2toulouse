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
            var redirectUrl=await _mastodon.GetAuthenticationUrl(GetRequestHost(), instance);
            return new RedirectResult(redirectUrl);
        }

        [Route("code")]
        [HttpGet]
        public ActionResult AuthFinish(string instance, string code)
        {
            return null;
        }
    }
}
