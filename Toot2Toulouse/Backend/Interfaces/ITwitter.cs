﻿using Toot2Toulouse.Backend.Configuration;

using Tweetinvi;
using Tweetinvi.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        //Task AuthTest();


        Task<ITweet> Tweet(string content, long? replyTo=null); // TODO: Media, Mentions

        Task<string> GetAuthenticationUrl(string baseUrl);

        Task<bool> FinishAuthentication(string query);

        void InitUser(TwitterClient userClient, UserConfiguration userConfiguration);
        Task Publish(Mastonet.Entities.Status toot);
    }
}
