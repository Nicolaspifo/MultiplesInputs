using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>Applies movement supplied by PlayerInputSource. It has no knowledge of keys.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputSource inputSource;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.4f;
        [SerializeField, Min(0.05f)] private float collisionHeight = 0.8f;

        public PlayerInputSource InputSource => inputSource;

        public void Configure(PlayerInputSource source, float speed)
        {
            inputSource = source;
            moveSpeed = speed;
        }

        private void Update()
        {
            if (inputSource != null)
            {
                Move(inputSource.Movement, Time.deltaTime);
            }
        }

        public void Move(Vector2 input, float deltaTime)
        {
            Vector2 normalizedInput = Vector2.ClampMagnitude(input, 1f);
            Vector3 displacement = new Vector3(normalizedInput.x, 0f, normalizedInput.y) * (moveSpeed * deltaTime);
            Vector3 current = transform.position;
            transform.position = MovementCollision.Resolve(transform, current, current + displacement, obstacleLayers, collisionRadius, collisionHeight);
        }
    }
}
