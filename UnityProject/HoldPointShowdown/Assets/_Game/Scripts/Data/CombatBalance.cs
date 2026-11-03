using UnityEngine;

namespace HoldPointShowdown.Data
{
    public static class CombatBalance
    {
        public const float StandardHealth = 100f;
        public const float StandardArmor = 50f;
        public const float ArmorAbsorptionFraction = 0.25f;
        // Armor takes 25% of each hit while health takes 75%. With 50 armor, health is the limiting pool.
        public const float StandardEffectiveHealth = StandardHealth / (1f - ArmorAbsorptionFraction);

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
            float minimumTtk = weapon.Rarity == ItemRarity.Legendary ? 1.15f : 1.2f;
            return dps >= 30f && dps <= 95f
                && unarmoredTtk >= minimumTtk
                && ttk >= minimumTtk
                && ttk <= 3f;
        }
    }
}
