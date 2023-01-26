﻿using Mastonet;
using Mastonet.Entities;

using System.Text.Json.Serialization;

using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

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

        //Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100);

        Task SendStatusMessageTo(Guid id, string? prefix, MessageCodes messageCode);

        Task<Account?> GetUserAccount(UserData userData);

        Task<Account?> GetUserAccount(MastodonClient mastodonClient);

        Task<List<Status>> GetNonPostedToots(Guid id);

        Task<List<Status>> GetTootsContaining(Guid id, string content, int limit = 100);

        Task<List<Status>> GetServiceTootsContaining(string content, int limit = 100, string? recipient = null);
    }
}