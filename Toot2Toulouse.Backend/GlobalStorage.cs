using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend
{
    public static class GlobalStorage
    {
        public static Dictionary<string, string> UserReplacements { get; set; }

        public static void FillGlobalReplacements(List<UserData> allUsers)
        {
            UserReplacements = new Dictionary<string, string>();

            foreach (var user in allUsers)
            {
                if (user.Config.Replacements == null) continue;
                var userReplacements = user.Config.Replacements.Where(q => q.Key.StartsWith("@"));
                if (userReplacements.Any())
                {
                    foreach (var replacement in userReplacements)
                    {
                        if (UserReplacements.ContainsKey(replacement.Key.ToLower())) continue;
                        UserReplacements.Add(replacement.Key.ToLower(), replacement.Value.ToLower());
                    }
                }
            }
        }
    }
}