using System;
using NetworkingLab.Player;
using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Local hit points with a basic respawn, used by the offline baseline.</summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;

        private int current;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float respawnAt;
        private bool dead;

        public int Current => current;
        public int Max => maxHealth;
        public bool IsDead => dead;
        public Transform DamageRoot => transform;

        public event Action<Health> Died;
        public event Action<Health> Respawned;

        public void Configure(int max, float delay)
        {
            maxHealth = Mathf.Max(1, max);
            respawnDelay = Mathf.Max(0f, delay);
            current = maxHealth;
        }

        private void Awake()
        {
            current = maxHealth;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (dead && Time.time >= respawnAt)
            {
                Respawn();
            }
        }

        public bool ApplyDamage(int amount)
        {
            if (dead || amount <= 0)
            {
                return false;
            }

            current = Mathf.Max(0, current - amount);
            if (current == 0)
            {
                Die();
            }

            return true;
        }

        private void Die()
        {
            dead = true;
            respawnAt = Time.time + respawnDelay;
            SetActiveState(false);
            CombatFx.SpawnExplosion(transform.position, CombatFx.TankColor);
            Died?.Invoke(this);
        }

        private void Respawn()
        {
            dead = false;
            current = maxHealth;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            SetActiveState(true);
            Respawned?.Invoke(this);
        }

        private void SetActiveState(bool value)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = value;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = value;
            }

            foreach (PlayerController controller in GetComponents<PlayerController>())
            {
                controller.enabled = value;
            }

            foreach (TankWeapon weapon in GetComponents<TankWeapon>())
            {
                weapon.enabled = value;
            }
        }
    }
}
