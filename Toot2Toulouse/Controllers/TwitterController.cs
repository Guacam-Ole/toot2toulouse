using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Linq.Expressions;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TwitterController : ControllerBase
    {
        private readonly ILogger<TwitterController> _logger;
        private readonly TootConfiguration _configuration;
        private readonly ITwitter _tweet;

        public TwitterController(ILogger<TwitterController> logger, ConfigReader configReader, ITwitter tweet)
        {
            _logger = logger;
            _configuration = configReader.Configuration;
            _tweet = tweet;
        }

        private string GetRequestHost()
        {
            return string.Format($"{Request.Scheme}://{Request.Host.Value}");
        }

        [Route("auth")]
        public async Task<ActionResult> AuthStart()
        {
            return new RedirectResult(await _tweet.GetAuthenticationUrlAsync(GetRequestHost()));
        }

        [Route("code")]
        [HttpGet]
        public async Task AuthFinish()
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString.Value)) return;
            await _tweet.FinishAuthenticationAsync(Request.QueryString.Value);
        }
    }
}
