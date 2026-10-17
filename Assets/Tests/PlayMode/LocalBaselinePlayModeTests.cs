using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetworkingLab.Player;
using NetworkingLab.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetworkingLab.Tests
{
    public sealed class LocalBaselinePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadBaseline()
        {
            SceneManager.LoadScene("LocalGameplayBaseline");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsFourPlayersGroundCameraAndUi()
        {
            Assert.AreEqual(4, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("Ground"));
            Assert.IsNotNull(Camera.main);
            Assert.IsNotNull(Object.FindFirstObjectByType<PowerUpMessageUI>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayersOneAndTwoUseArduinoAndThreeAndFourUseKeyboard()
        {
            ArduinoInputSource[] arduino = Object.FindObjectsByType<ArduinoInputSource>(FindObjectsSortMode.None)
                .OrderBy(source => source.PlayerIndex)
                .ToArray();
            Assert.AreEqual(2, arduino.Length, "Players 1 and 2 must be driven by the Arduino");
            Assert.AreEqual(0, arduino[0].PlayerIndex);
            Assert.AreEqual(1, arduino[1].PlayerIndex);

            PlayerInputSource[] keyboard = Object.FindObjectsByType<PlayerInputSource>(FindObjectsSortMode.None)
                .OrderBy(source => source.ActionMapName)
                .ToArray();
            Assert.AreEqual(2, keyboard.Length, "Players 3 and 4 must remain keyboard driven");
            Assert.AreEqual(new[] { "Player 3", "Player 4" }, keyboard.Select(source => source.ActionMapName).ToArray());

            Assert.IsNotNull(Object.FindFirstObjectByType<ArduinoSerialManager>(), "The Arduino serial manager must exist in the scene");
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardPlayersMoveFromTheirConfiguredInput()
        {
            var sources = Object.FindObjectsByType<PlayerInputSource>(FindObjectsSortMode.None)
                .OrderBy(source => source.ActionMapName)
                .ToArray();
            Key[] forwardKeys = { Key.I, Key.Numpad8 };

            for (int index = 0; index < sources.Length; index++)
            {
                PlayerController player = sources[index].GetComponent<PlayerController>();
                Assert.IsNotNull(player, sources[index].name + " has no PlayerController");
                Vector3 before = player.transform.position;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(forwardKeys[index]));
                yield return null;
                Assert.Greater(player.transform.position.z, before.z, player.name + " did not move from its configured key");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator KeyboardBindingsMatchPlayersThreeAndFour()
        {
            var expected = new Dictionary<string, string[]>
            {
                ["Player 3"] = new[] { "<Keyboard>/i", "<Keyboard>/k", "<Keyboard>/j", "<Keyboard>/l", "<Keyboard>/o" },
                ["Player 4"] = new[] { "<Keyboard>/numpad8", "<Keyboard>/numpad5", "<Keyboard>/numpad4", "<Keyboard>/numpad6", "<Keyboard>/numpad0" }
            };

            foreach (PlayerInputSource source in Object.FindObjectsByType<PlayerInputSource>(FindObjectsSortMode.None))
            {
                InputActionMap map = source.InputActions.FindActionMap(source.ActionMapName, true);
                string[] paths = map.bindings.Where(binding => !binding.isComposite).Select(binding => binding.path).ToArray();
                CollectionAssert.AreEquivalent(expected[source.ActionMapName], paths, source.ActionMapName);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryConfiguredPowerUpInputIdentifiesPlayerAndMessageClears()
        {
            PowerUpMessageUI ui = Object.FindFirstObjectByType<PowerUpMessageUI>();
            PlayerInputSource[] sources = Object.FindObjectsByType<PlayerInputSource>(FindObjectsSortMode.None)
                .OrderBy(source => source.ActionMapName)
                .ToArray();
            Key[] powerUpKeys = { Key.O, Key.Numpad0 };

            for (int index = 0; index < sources.Length; index++)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(powerUpKeys[index]));
                yield return null;
                Assert.AreEqual($"{sources[index].ActionMapName} activated PowerUp", ui.CurrentMessage);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                yield return null;
            }

            yield return new WaitForSeconds(2.1f);
            Assert.AreEqual(string.Empty, ui.CurrentMessage);
        }
    }
}
