using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonGame
{
    public class TwitchLoginController : MonoBehaviour
    {
        [Header("Scene Names")]
        public string gameSceneName = "BalloonGame";

        [Header("References")]
        public BalloonGameSettings settings;
        public BalloonLanguage language;
        public GameObject loadingGroup;
        public GameObject loginGroup;
        public Button loginButton;
        public Text statusText;
        public Text deviceCodeText;

        private TwitchAuthService authService;
        private bool loginInProgress;

        private void Start()
        {
            authService = TwitchAuthService.Instance;
            if (authService == null)
                authService = new GameObject("Twitch Authentication").AddComponent<TwitchAuthService>();
            authService.language = language;

            authService.StatusChanged += SetStatus;
            authService.DeviceCodeReady += ShowDeviceCode;
            authService.Authenticated += OnAuthenticated;
            authService.AuthenticationFailed += OnAuthenticationFailed;
            loginButton.onClick.AddListener(BeginLogin);

            SetLoading(true);
            StartCoroutine(CheckSavedLogin());
        }

        private void OnDestroy()
        {
            if (authService == null)
                return;
            authService.StatusChanged -= SetStatus;
            authService.DeviceCodeReady -= ShowDeviceCode;
            authService.Authenticated -= OnAuthenticated;
            authService.AuthenticationFailed -= OnAuthenticationFailed;
        }

        private IEnumerator CheckSavedLogin()
        {
            SetStatus(language.checkingSavedSessionStatus);
            bool valid = false;
            yield return authService.RestoreSavedSession(result => valid = result);
            if (valid)
            {
                OpenGame();
                yield break;
            }

            SetLoading(false);
            SetStatus(language.loginReady);
        }

        private void BeginLogin()
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.twitchClientId))
            {
                OnAuthenticationFailed(language.missingClientId);
                return;
            }

            loginInProgress = true;
            loginButton.interactable = false;
            deviceCodeText.text = string.Empty;
            authService.BeginDeviceLogin(settings.twitchClientId);
        }

        private void ShowDeviceCode(string userCode, string verificationUri)
        {
            deviceCodeText.text = userCode;
            loginButton.interactable = false;
        }

        private void OnAuthenticated(TwitchAuthSession session)
        {
            loginInProgress = false;
            OpenGame();
        }

        private void OnAuthenticationFailed(string message)
        {
            loginInProgress = false;
            SetStatus(message);
        }

        private void OpenGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void SetLoading(bool loading)
        {
            if (loadingGroup != null)
                loadingGroup.SetActive(loading);
            if (loginGroup != null)
                loginGroup.SetActive(!loading);
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
            if (loginButton != null && authService != null &&
                !authService.HasAuthenticatedSession && !loginInProgress)
                loginButton.interactable = true;
        }
    }
}
