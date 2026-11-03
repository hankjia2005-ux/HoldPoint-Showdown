using UnityEngine;

namespace HoldPointShowdown.Data
{
    public static class CombatBalance
    {
        public const float StandardHealth = 100f;
        public const float StandardArmor = 50f;
        // 50 armor points cover 50 incoming damage at 25% mitigation, adding 12.5 effective health.
        public const float StandardEffectiveHealth = StandardHealth + (StandardArmor * 0.25f);

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
            // The one-per-match legendary airdrop intentionally enters the 150-185 DPS comeback band.
            float minimumTtk = weapon.Rarity == ItemRarity.Legendary ? 0.55f : 0.75f;
            return dps >= 70f && dps <= 185f && ttk >= minimumTtk && ttk <= 2.25f;
        }
    }
}
