﻿using Mastonet;
using Mastonet.Entities;

using System.Text.Json.Serialization;

using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.Messages;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IMastodon
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Visibilites
        {
            Public,
            NotListed,
            OnlyFollowers,
            OnlyMentioned
        }

        Task SendStatusMessageToAsync(UserData user, string? prefix, MessageCodes? messageCode, string? additionalInfo);

        Task<Account?> GetUserAccountAsync(UserData userData);

        Task<Account?> GetUserAccountAsync(MastodonClient mastodonClient);

        Task<List<Status>> GetNonPostedTootsAsync(UserData user);

        Task<List<Status>> GetTootsContainingAsync(UserData user, string? content, int limit = 100);

        Task<List<Status>> GetServiceTootsContainingAsync(string content, int limit = 100, string? recipient = null);

        Task AssignLastTweetedIfMissingAsync(UserData user);

        Task<Status> GetSingleTootAsync(UserData user, string tootId);
    }
}