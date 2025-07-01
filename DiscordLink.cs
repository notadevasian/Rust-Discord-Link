using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Ext.WebSocketClient;
using Oxide.Core.Plugins;
using Newtonsoft.Json; 

namespace Oxide.Plugins
{
    [Info("DiscordLink", "Asian", "1.1.0")]
    [Description("Links verified users to Discord group via WebSocket API")]
    public class DiscordLink : CovalencePlugin
    {
        private WebSocketClientExtension _webSocketExt;
        private string ConnectionId => "verification";

        // Config properties
        private string WebSocketUrl => GetConfigValue("API", "WebSocketUrl", "ws://localhost:3000");
        private string DiscordGroup => GetConfigValue("Group", "Name", "discord");

        private string MsgAlreadyVerified => GetConfigValue("Messages", "AlreadyVerified", "You have already been verified.");
        private string MsgNowVerified => GetConfigValue("Messages", "NowVerified", "You have been verified and added to the group!");
        private string MsgNotVerified => GetConfigValue("Messages", "NotVerified", "You are not verified. Please link your account first.");
        private string MsgConnectionError => GetConfigValue("Messages", "ConnectionError", "Could not connect to verification service.");
        private string MsgParseError => GetConfigValue("Messages", "ParseError", "Error parsing the verification response.");
        private string MsgDiscordLink => GetConfigValue("Messages", "DiscordLink", "Link your Discord account at https://example.com");

        private void Init()
        {
            Puts("DiscordLink Loaded!");

            AddCovalenceCommand("discord", "CmdDiscord");
            AddCovalenceCommand("verify", "CmdVerify");

            if (!permission.GroupExists(DiscordGroup))
            {
                permission.CreateGroup(DiscordGroup, DiscordGroup, 0);
                Puts($"Created permission group '{DiscordGroup}'.");
            }

            // Get WebSocket extension
            _webSocketExt = Interface.Oxide.GetExtension<WebSocketClientExtension>();
            if (_webSocketExt == null)
            {
                PrintError("WebSocketClient extension not found! Please ensure it's installed.");
                return;
            }

            // Connect to WebSocket server
            Task.Run(async () => await ConnectToWebSocket());
        }

        private async Task ConnectToWebSocket()
        {
            try
            {
                bool connected = await _webSocketExt.ConnectAsync(ConnectionId, WebSocketUrl, this);
                if (connected)
                {
                    Puts($"Connected to WebSocket server: {WebSocketUrl}");
                }
                else
                {
                    PrintError($"Failed to connect to WebSocket server: {WebSocketUrl}");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error connecting to WebSocket: {ex.Message}");
            }
        }

        private void Unload()
        {
            // Disconnect from WebSocket when plugin unloads
            if (_webSocketExt != null && _webSocketExt.IsConnected(ConnectionId))
            {
                Task.Run(async () => await _webSocketExt.DisconnectAsync(ConnectionId));
            }
        }

        // WebSocket event handlers
        [HookMethod("OnWebSocketConnected")]
        private void OnWebSocketConnected(string connectionId)
        {
            if (connectionId == ConnectionId)
            {
                Puts("WebSocket verification service connected!");
            }
        }

        [HookMethod("OnWebSocketConnectionFailed")]
        private void OnWebSocketConnectionFailed(string connectionId, string error)
        {
            if (connectionId == ConnectionId)
            {
                PrintError($"WebSocket connection failed: {error}");
            }
        }

        [HookMethod("OnWebSocketMessage")]
        private void OnWebSocketMessage(string connectionId, string message)
        {
            if (connectionId == ConnectionId)
            {
                Puts($"Received verification data: {message}");
                ProcessVerificationData(message);
            }
        }

        [HookMethod("OnWebSocketDisconnected")]
        private void OnWebSocketDisconnected(string connectionId)
        {
            if (connectionId == ConnectionId)
            {
                PrintWarning("WebSocket verification service disconnected!");

                // Attempt to reconnect after 5 seconds
                timer.Once(5f, () => Task.Run(async () => await ConnectToWebSocket()));
            }
        }

        [HookMethod("OnWebSocketError")]
        private void OnWebSocketError(string connectionId, string error)
        {
            if (connectionId == ConnectionId)
            {
                PrintError($"WebSocket error: {error}");
            }
        }

        // Command handlers
        private void CmdDiscord(IPlayer player, string command, string[] args)
        {
            player.Reply(MsgDiscordLink);
        }

        private void CmdVerify(IPlayer player, string command, string[] args)
        {
            if (_webSocketExt == null || !_webSocketExt.IsConnected(ConnectionId))
            {
                player.Reply(MsgConnectionError);
                return;
            }

            // Send request for verification data (you can customize this message format)
            string requestMessage = $"{{\"action\":\"verify\",\"steamId\":\"{player.Id}\"}}";

            Task.Run(async () =>
            {
                bool sent = await _webSocketExt.SendMessageAsync(ConnectionId, requestMessage);
                if (!sent)
                {
                    player.Reply(MsgConnectionError);
                }
            });
        }


        private class WebSocketMessage
        {
            public string type { get; set; }
            public VerifiedUser data { get; set; }
        }

        private void ProcessVerificationData(string jsonResponse)
        {
            try
            {

                WebSocketMessage message = JsonConvert.DeserializeObject<WebSocketMessage>(jsonResponse);

                if (message != null && message.type == "user_verified" && message.data != null)
                {
                    ProcessUserVerification(message.data);
                }
                else
                {
                    PrintWarning($"Received unexpected WebSocket message type or format: {jsonResponse}");
                }
            }
            catch (JsonSerializationException ex)
            {
                PrintError($"JSON deserialization error: {ex.Message} (Input: {jsonResponse})");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to parse verification data: {ex.Message} (Input: {jsonResponse})");
                // Log the inner exception if available for more details
                if (ex.InnerException != null)
                {
                    PrintError($"Inner Exception: {ex.InnerException.Message}");
                    PrintError($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
            }
        }

        private void ProcessUserVerification(VerifiedUser user)
        {
            try
            {
                string steamId = user.steamId;
                var player = players.FindPlayerById(steamId);

                if (player != null && player.IsConnected)
                {
                    if (!permission.UserHasGroup(steamId, DiscordGroup))
                    {
                        permission.AddUserGroup(steamId, DiscordGroup);
                        player.Reply(MsgNowVerified);
                        Puts($"Verified user {user.steamName} ({steamId}) and added to {DiscordGroup} group");
                    }
                    else
                    {
                        player.Reply(MsgAlreadyVerified);
                    }
                }
                else
                {
                    // User not online, still add to group for when they connect
                    if (!permission.UserHasGroup(steamId, DiscordGroup))
                    {
                        permission.AddUserGroup(steamId, DiscordGroup);
                        Puts($"Verified offline user {user.steamName} ({steamId}) and added to {DiscordGroup} group");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error processing user verification: {ex.Message}");
            }
        }

        private class VerifiedUser
        {
            public int id { get; set; }
            public string steamId { get; set; }
            public string steamName { get; set; }
            public string steamAvatarUrl { get; set; }
            public string discordId { get; set; }
            public string discordUsername { get; set; }
            public string discordAvatar { get; set; }
        }

        // Configuration
        protected override void LoadDefaultConfig()
        {
            LogWarning("Generating new config file...");

            Config["API", "WebSocketUrl"] = "ws://localhost:3000/";
            Config["Group", "Name"] = "discord";

            Config["Messages", "AlreadyVerified"] = "You have already been verified.";
            Config["Messages", "NowVerified"] = "You have been verified and added to the group!";
            Config["Messages", "NotVerified"] = "You are not verified. Please link your account first.";
            Config["Messages", "ConnectionError"] = "Could not connect to verification service.";
            Config["Messages", "ParseError"] = "Error parsing the verification response.";
            Config["Messages", "DiscordLink"] = "Link your Discord account at https://example.com";

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
