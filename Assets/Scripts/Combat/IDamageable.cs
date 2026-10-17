using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Anything a projectile can damage. Shared by the local and networked tanks.</summary>
    public interface IDamageable
    {
        Transform DamageRoot { get; }

        bool ApplyDamage(int amount);
    }
}
