using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonGame
{
    public class GameHudController : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;
        public CommandManager commandManager;
        public TwitchChatService twitchService;
        public Text stateText;
        public Text roundText;
        public Text queueCountText;
        public Text queueListText;
        public Text currentPlayerText;
        public Text commandText;
        public Text connectionText;
        public Text resultText;
        public CanvasGroup resultGroup;

        private Coroutine resultRoutine;

        private void OnEnable()
        {
            if (commandManager != null)
                commandManager.QueueChanged += RefreshQueue;
            if (twitchService != null)
                twitchService.StatusChanged += SetConnectionStatus;
        }

        private void OnDisable()
        {
            if (commandManager != null)
                commandManager.QueueChanged -= RefreshQueue;
            if (twitchService != null)
                twitchService.StatusChanged -= SetConnectionStatus;
        }

        private void Start()
        {
            if (commandText != null)
                commandText.text = settings.command;
            SetConnectionStatus(twitchService != null ? twitchService.Status : settings.language.localTestMode);
            SetState(BalloonGameState.Waiting);
            SetCurrentPlayer(null);
            RefreshQueue();
            if (resultGroup != null)
                resultGroup.alpha = 0f;
        }

        public void SetState(BalloonGameState state)
        {
            if (stateText == null)
                return;
            switch (state)
            {
                case BalloonGameState.Inflating:
                    stateText.text = settings.language.inflating;
                    break;
                case BalloonGameState.CheckingExplosion:
                    stateText.text = settings.language.tensionRising;
                    break;
                case BalloonGameState.Exploding:
                    stateText.text = settings.language.exploded;
                    break;
                case BalloonGameState.Resetting:
                    stateText.text = settings.language.newRound;
                    break;
                default:
                    stateText.text = settings.language.waitingForPlayer;
                    break;
            }
        }

        public void SetRound(int roundNumber)
        {
            if (roundText != null)
                roundText.text = settings.language.roundFormat.Replace(
                    "{round}",
                    roundNumber.ToString("00"));
        }

        public void SetCurrentPlayer(ChatPlayer player)
        {
            if (currentPlayerText == null)
                return;
            if (player == null || settings == null || !settings.showPlayerNames)
            {
                currentPlayerText.text = string.Empty;
                return;
            }

            currentPlayerText.text = player.displayName;
            currentPlayerText.color = player.nameColor;
        }

        public void ShowResult(ChatPlayer player)
        {
            if (resultText == null || resultGroup == null)
                return;

            if (resultRoutine != null)
                StopCoroutine(resultRoutine);
            resultRoutine = StartCoroutine(ResultAnimation(player));
        }

        private IEnumerator ResultAnimation(ChatPlayer player)
        {
            resultText.text = settings.language.resultFormat.Replace(
                "{player}",
                player.displayName.ToUpperInvariant());
            resultText.color = player.nameColor;
            float elapsed = 0f;
            const float entrance = 0.24f;
            while (elapsed < entrance)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entrance);
                resultGroup.alpha = t;
                resultGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            resultGroup.alpha = 1f;
        }

        public void HideResult()
        {
            if (resultRoutine != null)
                StopCoroutine(resultRoutine);
            resultRoutine = null;
            if (resultGroup != null)
                resultGroup.alpha = 0f;
        }

        public void RefreshQueue()
        {
            if (commandManager == null)
                return;

            if (queueCountText != null)
                queueCountText.text = commandManager.QueueCount.ToString("00");
            if (queueListText == null)
                return;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            var preview = commandManager.GetQueuePreview(6);
            if (preview.Count == 0)
            {
                builder.Append(settings.language.emptyQueue);
            }
            else
            {
                for (int i = 0; i < preview.Count; i++)
                    builder.Append(i + 1).Append(".  ").Append(preview[i].displayName).AppendLine();
            }
            queueListText.text = builder.ToString().TrimEnd();
        }

        private void SetConnectionStatus(string value)
        {
            if (connectionText != null)
                connectionText.text = value;
        }
    }
}
