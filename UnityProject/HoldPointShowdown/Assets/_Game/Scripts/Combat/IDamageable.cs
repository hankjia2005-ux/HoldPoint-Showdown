using UnityEngine;

namespace HoldPointShowdown.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(float amount, Vector3 hitPoint, GameObject instigator);
    }
}
