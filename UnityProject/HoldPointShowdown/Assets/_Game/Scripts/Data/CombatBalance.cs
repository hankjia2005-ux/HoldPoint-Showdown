using UnityEngine;

namespace HoldPointShowdown.Data
{
    public static class CombatBalance
    {
        public const float StandardHealth = 100f;
        public const float StandardArmor = 50f;
        public const float StandardEffectiveHealth = StandardHealth + StandardArmor;

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
            return dps >= 70f && dps <= 185f && ttk >= 0.75f && ttk <= 2.25f;
        }
    }
}
