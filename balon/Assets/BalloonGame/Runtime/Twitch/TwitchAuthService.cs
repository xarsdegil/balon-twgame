using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BalloonGame
{
    public class TwitchAuthService : MonoBehaviour
    {
        private const string DeviceEndpoint = "https://id.twitch.tv/oauth2/device";
        private const string TokenEndpoint = "https://id.twitch.tv/oauth2/token";
        private const string ValidateEndpoint = "https://id.twitch.tv/oauth2/validate";
        private const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
        private const string RequiredScopes = "chat:read chat:edit";
        private static readonly string[] RequiredScopeList = { "chat:read", "chat:edit" };

        public static TwitchAuthService Instance { get; private set; }
        public BalloonLanguage language;
        public TwitchAuthSession Session { get; private set; }
        public bool HasAuthenticatedSession => Session != null && Session.HasAccessToken;

        public event Action<string> StatusChanged;
        public event Action<string, string> DeviceCodeReady;
        public event Action<TwitchAuthSession> Authenticated;
        public event Action<string> AuthenticationFailed;

        private Coroutine activeAuthentication;
        private Coroutine sessionValidation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator RestoreSavedSession(Action<bool> completed)
        {
            if (!TwitchTokenStore.TryLoad(out TwitchAuthSession saved))
            {
                completed?.Invoke(false);
                yield break;
            }

            Session = saved;
            bool valid = false;
            yield return ValidateCurrentSession(result => valid = result);
            if (!valid && Session.HasRefreshToken)
            {
                bool refreshed = false;
                yield return RefreshSession(result => refreshed = result);
                if (refreshed)
                    yield return ValidateCurrentSession(result => valid = result);
            }

            if (!valid)
            {
                Session = null;
                TwitchTokenStore.Delete();
            }
            else
            {
                StartHourlyValidation();
            }

            completed?.Invoke(valid);
        }

        public void BeginDeviceLogin(string clientId)
        {
            string normalizedClientId = (clientId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedClientId))
            {
                AuthenticationFailed?.Invoke(language.clientIdRequired);
                return;
            }

            if (activeAuthentication != null)
                StopCoroutine(activeAuthentication);
            activeAuthentication = StartCoroutine(DeviceLoginFlow(normalizedClientId));
        }

        public void CancelLogin()
        {
            if (activeAuthentication != null)
                StopCoroutine(activeAuthentication);
            activeAuthentication = null;
            SetStatus(language.loginCancelled);
        }

        public void SignOut()
        {
            CancelLogin();
            if (sessionValidation != null)
                StopCoroutine(sessionValidation);
            sessionValidation = null;
            Session = null;
            TwitchTokenStore.Delete();
        }

        private IEnumerator DeviceLoginFlow(string clientId)
        {
            TwitchTokenStore.RememberClientId(clientId);
            SetStatus(language.preparingConnection);

            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("scopes", RequiredScopes);

            using UnityWebRequest request = UnityWebRequest.Post(DeviceEndpoint, form);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Fail(ReadError(request, language.deviceCodeRequestFailed));
                yield break;
            }

            DeviceCodeResponse device = JsonUtility.FromJson<DeviceCodeResponse>(request.downloadHandler.text);
            if (device == null || string.IsNullOrWhiteSpace(device.device_code))
            {
                Fail(language.invalidDeviceCode);
                yield break;
            }

            DeviceCodeReady?.Invoke(device.user_code, device.verification_uri);
            SetStatus(language.approveInBrowser);
            Application.OpenURL(device.verification_uri);

            float deadline = Time.realtimeSinceStartup + Mathf.Max(1, device.expires_in);
            float interval = Mathf.Max(1, device.interval);
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(interval);

                WWWForm tokenForm = new WWWForm();
                tokenForm.AddField("client_id", clientId);
                tokenForm.AddField("scopes", RequiredScopes);
                tokenForm.AddField("device_code", device.device_code);
                tokenForm.AddField("grant_type", DeviceGrant);

                using UnityWebRequest tokenRequest = UnityWebRequest.Post(TokenEndpoint, tokenForm);
                yield return tokenRequest.SendWebRequest();

                if (tokenRequest.result == UnityWebRequest.Result.Success)
                {
                    TokenResponse token = JsonUtility.FromJson<TokenResponse>(tokenRequest.downloadHandler.text);
                    Session = new TwitchAuthSession
                    {
                        clientId = clientId,
                        accessToken = token.access_token,
                        refreshToken = token.refresh_token,
                        scopes = token.scope,
                        savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    bool valid = false;
                    yield return ValidateCurrentSession(result => valid = result);
                    if (!valid)
                    {
                        Fail(language.tokenValidationFailed);
                        yield break;
                    }

                    TwitchTokenStore.Save(Session);
                    StartHourlyValidation();
                    activeAuthentication = null;
                    SetStatus(language.loginComplete);
                    Authenticated?.Invoke(Session);
                    yield break;
                }

                OAuthErrorResponse error =
                    JsonUtility.FromJson<OAuthErrorResponse>(tokenRequest.downloadHandler.text);
                string message = error != null ? error.message : string.Empty;
                if (message.IndexOf("authorization_pending", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (message.IndexOf("slow_down", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    interval += 1f;
                    continue;
                }

                Fail(string.IsNullOrWhiteSpace(message) ? language.loginFailed : message);
                yield break;
            }

            Fail(language.loginCodeExpired);
        }

        private IEnumerator ValidateCurrentSession(Action<bool> completed)
        {
            if (Session == null || !Session.HasAccessToken)
            {
                completed?.Invoke(false);
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(ValidateEndpoint);
            request.SetRequestHeader("Authorization", "OAuth " + Session.accessToken);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false);
                yield break;
            }

            ValidateResponse response = JsonUtility.FromJson<ValidateResponse>(request.downloadHandler.text);
            string[] grantedScopes = response != null
                ? response.scopes ?? Array.Empty<string>()
                : Array.Empty<string>();
            bool hasRequiredScopes = Array.TrueForAll(
                RequiredScopeList,
                required => Array.Exists(grantedScopes, granted => granted == required));
            bool valid = response != null &&
                         !string.IsNullOrWhiteSpace(response.login) &&
                         response.client_id == Session.clientId &&
                         hasRequiredScopes;
            if (valid)
            {
                Session.login = response.login;
                Session.userId = response.user_id;
                Session.scopes = response.scopes;
                TwitchTokenStore.Save(Session);
            }

            completed?.Invoke(valid);
        }

        private IEnumerator RefreshSession(Action<bool> completed)
        {
            WWWForm form = new WWWForm();
            form.AddField("client_id", Session.clientId);
            form.AddField("grant_type", "refresh_token");
            form.AddField("refresh_token", Session.refreshToken);

            using UnityWebRequest request = UnityWebRequest.Post(TokenEndpoint, form);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false);
                yield break;
            }

            TokenResponse token = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            Session.accessToken = token.access_token;
            Session.refreshToken = token.refresh_token;
            Session.scopes = token.scope;
            Session.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            TwitchTokenStore.Save(Session);
            completed?.Invoke(true);
        }

        private void StartHourlyValidation()
        {
            if (sessionValidation != null)
                StopCoroutine(sessionValidation);
            sessionValidation = StartCoroutine(HourlyValidationLoop());
        }

        private IEnumerator HourlyValidationLoop()
        {
            while (Session != null)
            {
                yield return new WaitForSecondsRealtime(3600f);
                bool valid = false;
                yield return ValidateCurrentSession(result => valid = result);
                if (valid)
                    continue;

                bool refreshed = false;
                if (Session != null && Session.HasRefreshToken)
                    yield return RefreshSession(result => refreshed = result);
                if (refreshed)
                    yield return ValidateCurrentSession(result => valid = result);

                if (!valid)
                {
                    Session = null;
                    TwitchTokenStore.Delete();
                    AuthenticationFailed?.Invoke(language.sessionExpired);
                    sessionValidation = null;
                    if (SceneManager.GetActiveScene().name != "TwitchLogin")
                        SceneManager.LoadScene("TwitchLogin");
                    yield break;
                }
            }
        }

        private void SetStatus(string value)
        {
            StatusChanged?.Invoke(value);
        }

        private void Fail(string message)
        {
            activeAuthentication = null;
            SetStatus(message);
            AuthenticationFailed?.Invoke(message);
        }

        private static string ReadError(UnityWebRequest request, string fallback)
        {
            OAuthErrorResponse error = JsonUtility.FromJson<OAuthErrorResponse>(
                request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
            return error != null && !string.IsNullOrWhiteSpace(error.message)
                ? error.message
                : fallback;
        }

        [Serializable]
        private class DeviceCodeResponse
        {
            public string device_code;
            public int expires_in;
            public int interval;
            public string user_code;
            public string verification_uri;
        }

        [Serializable]
        private class TokenResponse
        {
            public string access_token;
            public int expires_in;
            public string refresh_token;
            public string[] scope;
            public string token_type;
        }

        [Serializable]
        private class ValidateResponse
        {
            public string client_id;
            public string login;
            public string[] scopes;
            public string user_id;
            public int expires_in;
        }

        [Serializable]
        private class OAuthErrorResponse
        {
            public int status;
            public string message;
        }
    }
}
