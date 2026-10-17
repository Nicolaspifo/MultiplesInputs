using NetworkingLab.Player;
using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Turns the tank to face the direction it is moving.</summary>
    public sealed class TankFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputSource inputSource;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField] private Vector3 direction = Vector3.forward;

        private bool hasDirection;

        private void Awake()
        {
            if (inputSource == null)
            {
                inputSource = GetComponent<PlayerInputSource>();
            }
        }

        public void SetDirection(Vector3 value)
        {
            value.y = 0f;
            if (value.sqrMagnitude > 0.0001f)
            {
                direction = value.normalized;
                hasDirection = true;
            }
        }

        private void Update()
        {
            Vector3 target = direction;
            if (inputSource != null)
            {
                Vector2 move = inputSource.Movement;
                if (move.sqrMagnitude > 0.0004f)
                {
                    target = new Vector3(move.x, 0f, move.y);
                    hasDirection = true;
                }
            }

            target.y = 0f;
            if (!hasDirection || target.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(target.normalized, Vector3.up);
            transform.rotation = turnSpeed <= 0f
                ? targetRotation
                : Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
