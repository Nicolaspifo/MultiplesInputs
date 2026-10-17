using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Straight-line shot. Only an authoritative instance applies damage.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float speed = 18f;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;
        [SerializeField, Min(1)] private int damage = 1;

        private Rigidbody body;
        private Vector3 direction = Vector3.forward;
        private Transform ownerRoot;
        private bool authoritative = true;
        private float dieAt;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            GetComponent<SphereCollider>().isTrigger = true;
        }

        public void Launch(Vector3 dir, Transform owner, bool applyDamage, float travelSpeed, float lifeSeconds, int hitDamage)
        {
            gameObject.SetActive(true);
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            ownerRoot = owner;
            authoritative = applyDamage;
            if (travelSpeed > 0f)
            {
                speed = travelSpeed;
            }

            if (lifeSeconds > 0f)
            {
                lifetime = lifeSeconds;
            }

            if (hitDamage > 0)
            {
                damage = hitDamage;
            }

            dieAt = Time.time + lifetime;
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
        }

        private void Update()
        {
            if (Time.time >= dieAt)
            {
                Impact();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>() != null)
            {
                return;
            }

            if (ownerRoot != null && (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot)))
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && authoritative)
            {
                target.ApplyDamage(damage);
            }

            Impact();
        }

        private void Impact()
        {
            CombatFx.SpawnExplosion(transform.position, CombatFx.ProjectileColor);
            Destroy(gameObject);
        }
    }
}
