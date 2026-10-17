using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>
    /// Player input driven by the Arduino. Reads its two axes and its button from
    /// <see cref="ArduinoSerialManager"/> using <see cref="playerIndex"/>.
    /// </summary>
    public sealed class ArduinoInputSource : PlayerInputBase
    {
        [Tooltip("0 = Jugador 1 (A0/A1 + pin 2), 1 = Jugador 2 (A2/A3 + pin 3).")]
        [SerializeField, Min(0)] private int playerIndex;

        [Tooltip("Zona muerta alrededor del centro del potenciometro (0 = sin zona muerta).")]
        [SerializeField, Range(0f, 0.9f)] private float deadzone = 0.15f;

        [Tooltip("Invierte el eje Y si el potenciometro esta montado al reves.")]
        [SerializeField] private bool invertY;

        private Vector2 movement;
        private bool wasButtonDown;

        public int PlayerIndex => playerIndex;

        public override Vector2 Movement => movement;

        public ArduinoInputSource ConfigurePlayer(int index, float deadzoneValue)
        {
            playerIndex = Mathf.Max(0, index);
            deadzone = Mathf.Clamp(deadzoneValue, 0f, 0.9f);
            return this;
        }

        private void Update()
        {
            ArduinoSerialManager manager = ArduinoSerialManager.Instance;
            if (manager == null)
            {
                movement = Vector2.zero;
                wasButtonDown = false;
                return;
            }

            Vector2 raw = manager.Movement(playerIndex);
            float x = ApplyDeadzone(raw.x);
            float y = ApplyDeadzone(raw.y);
            movement = new Vector2(x, invertY ? -y : y);

            bool buttonDown = manager.ButtonDown(playerIndex);
            if (buttonDown && !wasButtonDown)
            {
                RaisePowerUpPressed();
            }

            wasButtonDown = buttonDown;
        }

        private float ApplyDeadzone(float value)
        {
            float magnitude = Mathf.Abs(value);
            if (magnitude <= deadzone)
            {
                return 0f;
            }

            float scaled = (magnitude - deadzone) / (1f - deadzone);
            return Mathf.Sign(value) * scaled;
        }
    }
}
