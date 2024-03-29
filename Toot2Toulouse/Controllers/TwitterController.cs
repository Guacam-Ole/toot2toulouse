﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Linq.Expressions;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Interfaces;

using Toot2ToulouseWeb;

namespace Toot2Toulouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class TwitterController : ControllerBase
    {
        private readonly ILogger<TwitterController> _logger;
        private readonly ITwitterClientAuthentication _twitterClientAuthentication;
        private readonly TootConfiguration _config;
        private readonly ITwitter _tweet;

        public TwitterController(ILogger<TwitterController> logger, ConfigReader configReader, ITwitterClientAuthentication twitterClientAuthentication)
        {
            _logger = logger;
            _twitterClientAuthentication = twitterClientAuthentication;
            _config = configReader.Configuration;
        }

        private string GetRequestHost()
        {
            return string.Format($"{Request.Scheme}://{Request.Host.Value}");
        }

        [Route("auth")]
        public async Task<ActionResult> AuthStartAsync()
        {
            return new RedirectResult(await _twitterClientAuthentication.GetAuthenticationUrlAsync(GetRequestHost()));
        }

        [Route("code")]
        [HttpGet]
        public async Task<ActionResult> AuthFinishAsync()
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString.Value)) throw new ApiException(ApiException.ErrorTypes.Twitter, "query missing");
            var success=await _twitterClientAuthentication.FinishAuthenticationAsync(Request.QueryString.Value);
            if (!success) throw new ApiException(ApiException.ErrorTypes.Twitter, "auth error");
            return new RedirectResult($"/regfinished.{_config.App.Languages.Default}.html");
        }
    }
}
