using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BalloonGame
{
    public class LocalTestInput : MonoBehaviour
    {
        public CommandManager commandManager;
        public BalloonGameManager gameManager;
        public string[] testNames =
        {
            "Baloncu", "GeceKusu", "PikselKedi", "MorEjder", "KahveMolasi",
            "Ruzgar", "NeonTilki", "MinikDev", "AyIsigi", "KirmiziPanda"
        };

        private int nextUserIndex;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.spaceKey.wasPressedThisFrame)
                AddTestPlayer();
            if (keyboard.aKey.wasPressedThisFrame)
                AddTestGroup(5);
            if (keyboard.pKey.wasPressedThisFrame)
                gameManager.ForceExplosionOnNextTurn();
#else
            if (Input.GetKeyDown(KeyCode.Space))
                AddTestPlayer();
            if (Input.GetKeyDown(KeyCode.A))
                AddTestGroup(5);
            if (Input.GetKeyDown(KeyCode.P))
                gameManager.ForceExplosionOnNextTurn();
#endif
        }

        [ContextMenu("Add Test Player")]
        public void AddTestPlayer()
        {
            string name = testNames[nextUserIndex % testNames.Length];
            string id = "local-" + nextUserIndex;
            Color color = Color.HSVToRGB(Mathf.Repeat(nextUserIndex * 0.137f + 0.46f, 1f), 0.58f, 1f);
            nextUserIndex++;
            commandManager.TrySubmit(new ChatPlayer(id, name.ToLowerInvariant(), name, color));
        }

        [ContextMenu("Add Five Test Players")]
        public void AddFiveTestPlayers() => AddTestGroup(5);

        private void AddTestGroup(int count)
        {
            for (int i = 0; i < count; i++)
                AddTestPlayer();
        }
    }
}

