using System;
using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>
    /// Small input surface consumed by gameplay. Any source (keyboard, Arduino, ...)
    /// can implement it without the gameplay scripts knowing where the data comes from.
    /// </summary>
    public abstract class PlayerInputBase : MonoBehaviour
    {
        /// <summary>Movement on the XZ plane. X = left/right, Y = forward/backward.</summary>
        public abstract Vector2 Movement { get; }

        /// <summary>Raised once when the fire / power-up button is pressed.</summary>
        public event Action PowerUpPressed;

        protected void RaisePowerUpPressed()
        {
            PowerUpPressed?.Invoke();
        }
    }
}
