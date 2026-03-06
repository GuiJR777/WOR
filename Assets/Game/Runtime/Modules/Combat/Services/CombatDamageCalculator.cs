// Purpose: Calculates combat damage from attack data and stat-driven unit values.
using UnityEngine;

namespace WOR.Gameplay.Modules.Combat.Services {

    public static class CombatDamageCalculator {

        private const float DamageCriticalMultiplier = 3f;

        public static int CalculateDamage(
            AttackData attackData,
            UnitSettings attackerSettings,
            UnitSettings defenderSettings,
            out bool criticalHit) {
            criticalHit = false;

            if(attackData == null) {
                return 0;
            }

            if(attackerSettings == null) {
                return Mathf.Max(0, attackData.damage);
            }

            float attackScale = attackData.GetStrengthDamageScale();
            float attackerStrength = attackerSettings.GetStrength();
            float rawDamage = Mathf.Max(0f, attackerStrength * attackScale);

            float defenderDefense = defenderSettings != null ? defenderSettings.GetDefense() : 0f;
            float reducedDamage = Mathf.Max(0f, rawDamage - defenderDefense);

            float criticalChance = attackerSettings.GetCriticalChance();
            if(criticalChance > 0f && Random.value <= criticalChance) {
                criticalHit = true;
            }

            float finalDamage = criticalHit ? reducedDamage * DamageCriticalMultiplier : reducedDamage;
            return Mathf.Max(0, Mathf.RoundToInt(finalDamage));
        }

        public static int CalculateDamageFromInflictor(
            AttackData attackData,
            GameObject inflictor,
            UnitSettings defenderSettings,
            out bool criticalHit) {
            UnitSettings attackerSettings = inflictor != null ? inflictor.GetComponent<UnitSettings>() : null;
            return CalculateDamage(attackData, attackerSettings, defenderSettings, out criticalHit);
        }
    }
}
