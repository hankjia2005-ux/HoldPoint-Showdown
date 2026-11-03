using UnityEngine;

namespace HoldPointShowdown.Data
{
    public static class CombatBalance
    {
        public const float StandardHealth = 100f;
        public const float StandardArmor = 40f;
        public const float ArmorDamageRatio = 1f;
        public const float HeadMultiplier = 1.5f;
        public const float TorsoMultiplier = 1f;
        public const float LimbMultiplier = 0.8f;
        public const float StandardEffectiveHealth = StandardHealth + (StandardArmor * ArmorDamageRatio);

        public static float CalculateBodyDps(WeaponConfig weapon)
        {
            return weapon == null ? 0f : weapon.Damage * weapon.Pellets * weapon.ShotsPerSecond;
        }

        public static int CalculateShotsToEliminate(WeaponConfig weapon, float effectiveHealth = StandardEffectiveHealth)
        {
            if (weapon == null || weapon.Damage <= 0f || weapon.Pellets <= 0 || effectiveHealth <= 0f)
            {
                return 0;
            }

            return Mathf.CeilToInt(effectiveHealth / (weapon.Damage * weapon.Pellets));
        }

        public static float CalculateIdealTimeToEliminate(WeaponConfig weapon, float effectiveHealth = StandardEffectiveHealth)
        {
            int shots = CalculateShotsToEliminate(weapon, effectiveHealth);
            if (shots <= 1 || weapon == null || weapon.ShotsPerSecond <= 0f)
            {
                return 0f;
            }

            return (shots - 1) / weapon.ShotsPerSecond;
        }

        public static bool IsWithinDemoBalanceEnvelope(WeaponConfig weapon)
        {
            if (weapon == null)
            {
                return false;
            }

            float dps = CalculateBodyDps(weapon);
            float ttk = CalculateIdealTimeToEliminate(weapon);
            float unarmoredTtk = CalculateIdealTimeToEliminate(weapon, StandardHealth);
            float minimumTtk = weapon.Rarity >= ItemRarity.Epic ? 0.7f : 1.1f;
            return dps >= 30f && dps <= 120f
                && unarmoredTtk >= 0.65f
                && ttk >= minimumTtk
                && ttk <= 3f;
        }
    }
}
