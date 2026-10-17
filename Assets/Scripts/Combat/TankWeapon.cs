using NetworkingLab.Player;
using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Local tank gun: aims with movement and fires on the PowerUp action.</summary>
    public sealed class TankWeapon : MonoBehaviour
    {
        [SerializeField] private PlayerInputSource inputSource;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform barrel;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0.05f)] private float cooldown = 0.35f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
        [SerializeField, Min(1)] private int damage = 1;

        private Vector3 aimDirection = Vector3.forward;
        private float nextFireTime;
        private bool subscribed;

        public void Configure(PlayerInputSource source, GameObject prefab, Transform barrelTransform, Transform muzzleTransform)
        {
            inputSource = source;
            projectilePrefab = prefab;
            barrel = barrelTransform;
            muzzle = muzzleTransform;
            EnsureSubscribed();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        private void OnDisable()
        {
            if (subscribed && inputSource != null)
            {
                inputSource.PowerUpPressed -= Fire;
                subscribed = false;
            }
        }

        private void EnsureSubscribed()
        {
            if (subscribed || inputSource == null)
            {
                return;
            }

            inputSource.PowerUpPressed += Fire;
            subscribed = true;
        }

        private void Update()
        {
            if (inputSource == null)
            {
                return;
            }

            Vector2 move = inputSource.Movement;
            if (move.sqrMagnitude > 0.0004f)
            {
                aimDirection = new Vector3(move.x, 0f, move.y).normalized;
            }

            Transform aimBarrel = OwnsTransform(barrel) ? barrel : null;
            if (aimBarrel != null && aimDirection.sqrMagnitude > 0.0001f)
            {
                aimBarrel.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }
        }

        private bool OwnsTransform(Transform target)
        {
            return target != null && (target == transform || target.IsChildOf(transform));
        }

        public void Fire()
        {
            if (projectilePrefab == null || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + cooldown;
            Transform fireBarrel = OwnsTransform(barrel) ? barrel : null;
            Transform fireMuzzle = OwnsTransform(muzzle) ? muzzle : null;
            Vector3 direction = fireBarrel != null ? fireBarrel.forward : aimDirection;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            Vector3 origin = fireMuzzle != null ? fireMuzzle.position : FallbackOrigin(direction);
            GameObject shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
            if (shot.TryGetComponent(out Projectile projectile))
            {
                projectile.Launch(direction, transform, true, projectileSpeed, projectileLifetime, damage);
            }
        }

        private Vector3 FallbackOrigin(Vector3 direction)
        {
            float height = 0.6f;
            Collider body = GetComponent<Collider>();
            if (body != null)
            {
                height = Mathf.Max(0.35f, body.bounds.extents.y);
            }

            return transform.position + Vector3.up * height + direction * 0.9f;
        }
    }
}
