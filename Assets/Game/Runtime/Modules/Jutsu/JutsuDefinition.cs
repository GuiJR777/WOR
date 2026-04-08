// Purpose: Defines configurable jutsu data as ScriptableObject assets for player loadout slots.
using System.Collections.Generic;
using UnityEngine;

namespace WOR.Gameplay.Modules.Jutsu {

    public enum JutsuStyle {
        Shinobi = 0,
        Shadow = 10,
        Poison = 20,
        Water = 30,
        Fire = 40,
        Wind = 50,
        Earth = 60,
        Genjutsu = 70,
        Thunder = 80,
    }

    public enum JutsuSpawnPosition {
        InFrontOfPlayer = 0,
        AroundPlayerSmallRadius = 10,
        ClosestEnemy = 20,
        FarthestEnemy = 30,
    }

    [CreateAssetMenu(fileName = "JutsuDefinition", menuName = "WOR/Jutsu/Jutsu Definition")]
    public class JutsuDefinition : ScriptableObject {

        private const int MaxStyleCount = 2;
        private const int MinDamage = 0;

        [Header("Identity")]
        public string jutsuName = "New Jutsu";
        public List<JutsuStyle> styles = new List<JutsuStyle>();

        [Header("UI")]
        public Sprite uiElementImage;
        public string uiAnimation;

        [Header("Player")]
        public string playerAnimation;
        public bool switchPlayerToAiControlled;

        [Header("Combat")]
        [Min(MinDamage)] public int damage = 10;
        public bool useCasterStatsForDamage;
        public AttackData attackData = CreateDefaultAttackData();

        [Header("Spawn")]
        public List<GameObject> spawnPrefabs = new List<GameObject>();
        public JutsuSpawnPosition spawnPosition = JutsuSpawnPosition.InFrontOfPlayer;

        public bool UsesCasterStatsForDamage => useCasterStatsForDamage;

        public AttackData BuildAttackData(GameObject inflictor) {
            AttackData source = attackData ?? CreateDefaultAttackData();
            AttackData copy = CloneAttackData(source);

            copy.name = string.IsNullOrWhiteSpace(copy.name) ? jutsuName : copy.name;
            copy.damage = Mathf.Max(MinDamage, damage);
            copy.inflictor = inflictor;

            if(!useCasterStatsForDamage) {
                copy.strengthDamageScale = 0f;
            }

            return copy;
        }

        private void OnValidate() {
            if(styles == null) {
                styles = new List<JutsuStyle>();
            }

            for(int i = styles.Count - 1; i >= 0; i--) {
                JutsuStyle style = styles[i];
                for(int j = i - 1; j >= 0; j--) {
                    if(styles[j] == style) {
                        styles.RemoveAt(i);
                        break;
                    }
                }
            }

            while(styles.Count > MaxStyleCount) {
                styles.RemoveAt(styles.Count - 1);
            }

            if(spawnPrefabs == null) {
                spawnPrefabs = new List<GameObject>();
            }

            damage = Mathf.Max(MinDamage, damage);
            if(attackData == null) {
                attackData = CreateDefaultAttackData();
            }
            attackData.damage = damage;
        }

        private static AttackData CloneAttackData(AttackData source) {
            if(source == null) {
                return CreateDefaultAttackData();
            }

            AttackData copy = new AttackData(
                source.name,
                source.damage,
                source.inflictor,
                source.attackType,
                source.knockdown,
                source.sfx);

            copy.strengthDamageScale = source.GetStrengthDamageScale();
            copy.animationState = source.animationState;
            copy.knockdownLaunchHorizontalForce = source.knockdownLaunchHorizontalForce;
            copy.knockdownLaunchVerticalForce = source.knockdownLaunchVerticalForce;
            copy.applyKnockback = source.applyKnockback;
            copy.knockbackForce = source.knockbackForce;
            copy.knockbackVerticalForce = source.knockbackVerticalForce;
            copy.knockbackDuration = source.knockbackDuration;
            copy.attackerForwardDistance = source.attackerForwardDistance;
            copy.attackerForwardDuration = source.attackerForwardDuration;
            copy.attackerHopVerticalForce = source.attackerHopVerticalForce;
            copy.attackerHopOnlyWhenGrounded = source.attackerHopOnlyWhenGrounded;
            copy.conditionType = source.conditionType;
            copy.conditionCharge = source.GetConditionCharge();
            return copy;
        }

        private static AttackData CreateDefaultAttackData() {
            return new AttackData("Jutsu Attack", 10, null, ATTACKTYPE.PUNCH, false);
        }
    }
}
