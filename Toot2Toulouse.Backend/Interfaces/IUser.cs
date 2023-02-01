﻿using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IUser
    {
        UserData? GetUser(Guid id, string hash);
        UserData? ExportUserData(UserData userData);

        void UpdateUser(UserData user);
        void Block(Guid userId, UserData.BlockReasons reason);
        void Unblock(Guid userId);
    }
}
