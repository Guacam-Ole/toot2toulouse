using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Linq.Expressions;

using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TwitterAuthController : ControllerBase
    {
        private readonly ILogger<TwitterAuthController> _logger;
        private readonly ITootConfiguration _configuration;
        private readonly ITwitter _tweet;

        public TwitterAuthController(ILogger<TwitterAuthController> logger, ITootConfiguration configuration, ITwitter tweet)
        {
            _logger = logger;
            _configuration = configuration;
            _tweet = tweet;
        }

        private string GetRequestHost()
        {
            return string.Format($"{Request.Scheme}://{Request.Host.Value}");
        }

        [Route("init")]
        public async Task<ActionResult> InitRequest()
        {
       //     var secrets = _configuration.GetSecrets();
            return new RedirectResult(await _tweet.GetAuthenticationUrl(GetRequestHost()));
        }

        [HttpGet]
        public async Task Get()
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString.Value)) return;
            await _tweet.FinishAuthentication(Request.QueryString.Value);

                    
        }
    }
}
