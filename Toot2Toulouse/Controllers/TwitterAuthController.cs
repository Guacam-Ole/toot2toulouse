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
    public class TwitterAuthController : ControllerBase
    {
        private readonly ILogger<TwitterAuthController> _logger;
        private readonly TootConfiguration _configuration;
        private readonly ITwitter _tweet;

        public TwitterAuthController(ILogger<TwitterAuthController> logger, ConfigReader configReader, ITwitter tweet)
        {
            _logger = logger;
            _configuration = configReader.Configuration;
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
            return new RedirectResult(await _tweet.GetAuthenticationUrlAsync(GetRequestHost()));
        }

        [HttpGet]
        public async Task Get()
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString.Value)) return;
            await _tweet.FinishAuthenticationAsync(Request.QueryString.Value);

                    
        }
    }
}
