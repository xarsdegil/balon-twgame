using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BalloonGame
{
    public class TwitchChatService : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;

        [Header("Runtime Status (read only)")]
        [SerializeField] private bool isConnected;
        [SerializeField] private string status = "";

        public event Action<ChatPlayer, string> MessageReceived;
        public event Action<string> StatusChanged;
        public bool IsConnected => isConnected;
        public string Status => status;

        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private CancellationTokenSource cancellation;
        private TcpClient client;
        private StreamWriter writer;

        private void Start()
        {
            if (settings != null && settings.connectToTwitchOnStart)
                Connect();
            else
                SetStatus(settings != null ? settings.language.localTestMode : string.Empty, false);
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
                action?.Invoke();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        [ContextMenu("Connect to Twitch")]
        public void Connect()
        {
            if (settings == null)
            {
                SetStatus(string.Empty, false);
                return;
            }

            TwitchAuthSession session = TwitchAuthService.Instance != null
                ? TwitchAuthService.Instance.Session
                : null;
            string channel = FirstNonEmpty(
                Environment.GetEnvironmentVariable("TWITCH_CHANNEL"),
                FirstNonEmpty(settings.twitchChannel, session != null ? session.login : string.Empty))
                .Trim().TrimStart('#').ToLowerInvariant();
            string username = FirstNonEmpty(
                Environment.GetEnvironmentVariable("TWITCH_BOT_USERNAME"),
                FirstNonEmpty(settings.twitchUsername, session != null ? session.login : string.Empty))
                .Trim().ToLowerInvariant();
            string token = FirstNonEmpty(
                Environment.GetEnvironmentVariable("TWITCH_OAUTH_TOKEN"),
                FirstNonEmpty(session != null ? session.accessToken : string.Empty, settings.oauthToken))
                .Trim();

            if (string.IsNullOrWhiteSpace(channel))
            {
                SetStatus(settings.language.twitchChannelMissing, false);
                return;
            }
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
            {
                SetStatus(settings.language.twitchCredentialsMissing, false);
                return;
            }

            Disconnect();
            cancellation = new CancellationTokenSource();
            _ = RunConnectionAsync(channel, username, token, cancellation.Token);
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            writer?.Dispose();
            writer = null;
            client?.Close();
            client = null;
            if (isActiveAndEnabled)
                SetStatus(
                    settings != null && settings.language != null
                        ? settings.language.disconnected
                        : string.Empty,
                    false);
        }

        public bool SendChatMessage(string message)
        {
            if (!isConnected || writer == null || settings == null)
                return false;

            string channel = FirstNonEmpty(
                Environment.GetEnvironmentVariable("TWITCH_CHANNEL"),
                FirstNonEmpty(
                    settings.twitchChannel,
                    TwitchAuthService.Instance != null && TwitchAuthService.Instance.Session != null
                        ? TwitchAuthService.Instance.Session.login
                        : string.Empty))
                .Trim().TrimStart('#').ToLowerInvariant();
            string sanitized = (message ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
            if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(sanitized))
                return false;

            _ = writer.WriteLineAsync("PRIVMSG #" + channel + " :" + sanitized);
            return true;
        }

        private async Task RunConnectionAsync(string channel, string username, string accessToken,
            CancellationToken token)
        {
            try
            {
                EnqueueStatus(settings.language.connecting, false);
                client = new TcpClient();
                await client.ConnectAsync("irc.chat.twitch.tv", 6697);

                SslStream ssl = new SslStream(client.GetStream(), false);
                await ssl.AuthenticateAsClientAsync("irc.chat.twitch.tv");
                StreamReader reader = new StreamReader(ssl, Encoding.UTF8);
                writer = new StreamWriter(ssl, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };

                string pass = accessToken.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase)
                    ? accessToken
                    : "oauth:" + accessToken;
                await writer.WriteLineAsync("PASS " + pass);
                await writer.WriteLineAsync("NICK " + username);
                await writer.WriteLineAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
                await writer.WriteLineAsync("JOIN #" + channel);
                EnqueueStatus(
                    settings.language.connectedToChannel.Replace("{channel}", channel),
                    true);

                while (!token.IsCancellationRequested && client.Connected)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null)
                        break;
                    if (line.StartsWith("PING", StringComparison.Ordinal))
                    {
                        await writer.WriteLineAsync("PONG :tmi.twitch.tv");
                        continue;
                    }

                    if (TryParsePrivMessage(line, out ChatPlayer player, out string message))
                        mainThreadActions.Enqueue(() => MessageReceived?.Invoke(player, message));
                }
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested)
                    EnqueueStatus(
                        settings.language.twitchError.Replace("{error}", exception.Message),
                        false);
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    EnqueueStatus(settings.language.connectionLost, false);
            }
        }

        private bool TryParsePrivMessage(string line, out ChatPlayer player, out string message)
        {
            player = null;
            message = null;
            int commandIndex = line.IndexOf(" PRIVMSG #", StringComparison.Ordinal);
            if (commandIndex < 0)
                return false;

            Dictionary<string, string> tags = ParseTags(line);
            int messageIndex = line.IndexOf(" :", commandIndex, StringComparison.Ordinal);
            if (messageIndex < 0)
                return false;

            string prefix = line.StartsWith("@", StringComparison.Ordinal)
                ? line.Substring(line.IndexOf(' ') + 1)
                : line;
            int userStart = prefix.IndexOf(':') + 1;
            int userEnd = prefix.IndexOf('!', userStart);
            string username = userStart > 0 && userEnd > userStart
                ? prefix.Substring(userStart, userEnd - userStart)
                : settings.language.unknownViewer;

            tags.TryGetValue("user-id", out string userId);
            tags.TryGetValue("display-name", out string displayName);
            tags.TryGetValue("color", out string colorHex);
            if (string.IsNullOrWhiteSpace(userId))
                userId = username.ToLowerInvariant();

            Color nameColor = new Color32(95, 232, 206, 255);
            if (!string.IsNullOrWhiteSpace(colorHex))
                ColorUtility.TryParseHtmlString(colorHex, out nameColor);

            player = new ChatPlayer(userId, username, displayName, nameColor);
            message = line.Substring(messageIndex + 2);
            return true;
        }

        private static Dictionary<string, string> ParseTags(string line)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!line.StartsWith("@", StringComparison.Ordinal))
                return result;
            int end = line.IndexOf(' ');
            if (end < 0)
                return result;

            string[] pairs = line.Substring(1, end - 1).Split(';');
            foreach (string pair in pairs)
            {
                int separator = pair.IndexOf('=');
                if (separator > 0)
                    result[pair.Substring(0, separator)] = pair.Substring(separator + 1);
            }
            return result;
        }

        private void EnqueueStatus(string value, bool connected)
        {
            mainThreadActions.Enqueue(() => SetStatus(value, connected));
        }

        private void SetStatus(string value, bool connected)
        {
            status = value;
            isConnected = connected;
            StatusChanged?.Invoke(value);
        }

        private static string FirstNonEmpty(string preferred, string fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? fallback ?? string.Empty : preferred;
        }
    }
}
