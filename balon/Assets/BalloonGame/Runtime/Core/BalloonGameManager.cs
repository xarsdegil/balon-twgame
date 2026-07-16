using System.Collections;
using UnityEngine;

namespace BalloonGame
{
    public class BalloonGameManager : MonoBehaviour
    {
        [Header("References")]
        public BalloonGameSettings settings;
        public CommandManager commandManager;
        public BalloonController balloonController;
        public GameHudController hud;
        public TwitchChatService twitchChatService;

        [Header("Runtime (read only)")]
        public BalloonGameState state = BalloonGameState.Waiting;
        public BalloonRoundData round = new BalloonRoundData();

        private Coroutine gameLoop;

        private void Start()
        {
            if (settings == null || commandManager == null || balloonController == null || hud == null)
            {
                Debug.LogError("Balloon Game references are incomplete. Rebuild the prototype scene from Tools > Balloon Game.");
                enabled = false;
                return;
            }

            Application.targetFrameRate = settings.targetFrameRate;
            Application.runInBackground = settings.runInBackground;
#if !UNITY_EDITOR
            Screen.SetResolution(
                settings.squareResolution,
                settings.squareResolution,
                FullScreenMode.Windowed);
#endif
            gameLoop = StartCoroutine(RunGameLoop());
        }

        private IEnumerator RunGameLoop()
        {
            StartNewRound();
            while (true)
            {
                SetState(BalloonGameState.Waiting);
                ChatPlayer player;
                while (!commandManager.TryGetNextPlayer(out player))
                    yield return null;

                hud.SetCurrentPlayer(player);
                SetState(BalloonGameState.Inflating);

                float visualRoom = Mathf.InverseLerp(
                    settings.maximumVisualScale,
                    1f,
                    round.currentBalloonSize);
                float growth = Random.Range(settings.minimumGrowth, settings.maximumGrowth);
                growth *= Mathf.Lerp(0.32f, 1f, visualRoom);
                round.currentBalloonSize = Mathf.Min(
                    settings.maximumVisualScale,
                    round.currentBalloonSize + growth);

                float danger = Mathf.Clamp01((float)(round.currentInflationCount + 1) /
                                              Mathf.Max(1, settings.maximumInflations));
                yield return balloonController.Inflate(growth, danger);

                round.currentInflationCount++;
                round.lastPlayerId = player.userId;
                SetState(BalloonGameState.CheckingExplosion);

                if (round.currentInflationCount >= round.explosionThreshold)
                {
                    SetState(BalloonGameState.Exploding);
                    yield return balloonController.Explode();
                    hud.ShowResult(player);
                    AnnounceExplosion(player);
                    yield return new WaitForSecondsRealtime(settings.resultDisplayDuration);
                    SetState(BalloonGameState.Resetting);
                    StartNewRound();
                    yield return new WaitForSecondsRealtime(0.35f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(settings.delayBetweenPlayers);
                    hud.SetCurrentPlayer(null);
                }
            }
        }

        private void StartNewRound()
        {
            round.roundNumber++;
            round.currentInflationCount = 0;
            round.currentBalloonSize = 1f;
            round.explosionThreshold = Random.Range(settings.minimumInflations, settings.maximumInflations + 1);
            round.lastPlayerId = null;
            commandManager.BeginNewRound(settings.clearQueueAfterExplosion);
            balloonController.ResetBalloon();
            hud.HideResult();
            hud.SetRound(round.roundNumber);
            hud.SetCurrentPlayer(null);
        }

        private void SetState(BalloonGameState nextState)
        {
            state = nextState;
            hud.SetState(nextState);
        }

        private void AnnounceExplosion(ChatPlayer player)
        {
            if (player == null ||
                string.IsNullOrWhiteSpace(player.userId) ||
                player.userId.StartsWith("local-", System.StringComparison.OrdinalIgnoreCase) ||
                twitchChatService == null ||
                !twitchChatService.IsConnected)
                return;

            string displayName = string.IsNullOrWhiteSpace(player.displayName)
                ? player.username
                : player.displayName;
            string message = string.IsNullOrWhiteSpace(settings.explosionChatMessage)
                ? settings.language.defaultExplosionChatMessage
                : settings.explosionChatMessage;
            message = message
                .Replace("{player}", displayName)
                .Replace("{username}", player.username ?? displayName);
            twitchChatService.SendChatMessage(message);
        }

        [ContextMenu("Force Explosion On Next Turn")]
        public void ForceExplosionOnNextTurn()
        {
            round.explosionThreshold = round.currentInflationCount + 1;
        }
    }
}
