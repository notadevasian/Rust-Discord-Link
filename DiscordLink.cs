using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using TinyJSON;
using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("DiscordLink", "Asian", "1.0.0")]
    [Description("Links verified users to Discord group based on external API")]
    public class DiscordLink : CovalencePlugin
    {
        private string ApiUrl => GetConfigValue("API", "VerifiedUsersUrl", "http://localhost:3000/api/verified-users");
        private string DiscordGroup => GetConfigValue("Group", "Name", "discord");

        private string MsgAlreadyVerified => GetConfigValue("Messages", "AlreadyVerified", "You have already been verified.");
        private string MsgNowVerified => GetConfigValue("Messages", "NowVerified", "You have been verified and added to the group!");
        private string MsgNotVerified => GetConfigValue("Messages", "NotVerified", "You are not verified. Please link your account first.");
        private string MsgApiError => GetConfigValue("Messages", "ApiError", "Could not verify your account (HTTP {code})");
        private string MsgParseError => GetConfigValue("Messages", "ParseError", "Error parsing the verification response.");
        private string MsgDiscordLink => GetConfigValue("Messages", "DiscordLink", "Link your Discord account at https://example.com");

        private void Init()
        {
            Puts("Discord Link Loaded Correctly!");

            AddCovalenceCommand("discord", "CmdDiscord");
            AddCovalenceCommand("verify", "CmdVerify");

            if (!permission.GroupExists(DiscordGroup))
            {
                permission.CreateGroup(DiscordGroup, DiscordGroup, 0);
                Puts($"Created permission group '{DiscordGroup}'.");
            }
        }

        private void CmdDiscord(IPlayer player, string command, string[] args)
        {
            player.Reply(MsgDiscordLink);
        }

        private void CmdVerify(IPlayer player, string command, string[] args)
        {
            webrequest.Enqueue(ApiUrl, null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response))
                {
                    player.Reply(MsgApiError.Replace("{code}", code.ToString()));
                    return;
                }

                try
                {
                    var users = TinyJSON.JSON.Load(response).Make<List<VerifiedUser>>();
                    string steamId = player.Id;

                    foreach (var user in users)
                    {
                        if (user.steamId == steamId)
                        {
                            if (!permission.UserHasGroup(steamId, DiscordGroup))
                            {
                                permission.AddUserGroup(steamId, DiscordGroup);
                                player.Reply(MsgNowVerified);
                            }
                            else
                            {
                                player.Reply(MsgAlreadyVerified);
                            }
                            return;
                        }
                    }

                    player.Reply(MsgNotVerified);
                }
                catch (Exception ex)
                {
                    player.Reply(MsgParseError);
                    PrintError($"Failed to parse JSON response: {ex.Message}");
                }

            }, this, RequestMethod.GET);
        }

        private class VerifiedUser
        {
            public int id;
            public string steamId;
            public string steamName;
            public string steamAvatarUrl;
            public string discordId;
            public string discordUsername;
            public string discordAvatar;
        }

        protected override void LoadDefaultConfig()
        {
            LogWarning("Generating new config file...");

            Config["API", "VerifiedUsersUrl"] = "http://localhost:3000/api/verified-users";
            Config["Group", "Name"] = "discord";

            Config["Messages", "AlreadyVerified"] = "⚠️ You have already been verified.";
            Config["Messages", "NowVerified"] = "✅ You have been verified and added to the group!";
            Config["Messages", "NotVerified"] = "❌ You are not verified. Please link your account first.";
            Config["Messages", "ApiError"] = "❌ Could not verify your account (HTTP {code})";
            Config["Messages", "ParseError"] = "❌ Error parsing the verification response.";
            Config["Messages", "DiscordLink"] = "🔗 Link your Discord account at https://example.com";

            SaveConfig();
        }

        private T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            if (Config[category] is Dictionary<string, object> dict && dict.TryGetValue(setting, out var value))
                return (T)Convert.ChangeType(value, typeof(T));
            return defaultValue;
        }
    }
}
