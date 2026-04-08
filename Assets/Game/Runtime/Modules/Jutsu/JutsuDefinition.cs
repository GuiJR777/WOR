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
        private const float MinSlowMotionTimeScale = 0.01f;
        private const float MaxSlowMotionTimeScale = 1f;

        [Header("Identity")]
        public string jutsuName = "New Jutsu";
        public List<JutsuStyle> styles = new List<JutsuStyle>();

        [Header("UI")]
        public Sprite uiElementImage;
        public string uiAnimation;

        [Header("Player")]
        [Tooltip("Nome do estado/clip de animacao do player que sera tocado ao usar o jutsu.")]
        public string playerAnimation;
        [Tooltip("Quando ativo, o player entra no estado ControledByAI ao castar este jutsu.")]
        public bool switchPlayerToAiControlled;

        [Header("Timing")]
        [Min(0f)]
        [Tooltip("Atraso, em segundos, antes de spawnar os prefabs configurados do jutsu.")]
        public float spawnDelaySeconds;
        [Min(0f)]
        [Tooltip("Tempo maximo, em segundos, para manter o player em ControledByAI apos o cast. 0 = sem retorno automatico.")]
        public float aiControlledDurationSeconds;
        
        [Header("Slow Motion")]
        [Tooltip("Quando ativo, aplica slow motion ao castar este jutsu.")]
        public bool enableSlowMotion;
        [Range(MinSlowMotionTimeScale, MaxSlowMotionTimeScale)]
        [Tooltip("Escala de tempo durante o slow motion. 1 = sem efeito, valores menores deixam o tempo mais lento.")]
        public float slowMotionTimeScale = 0.25f;
        [Min(0f)]
        [Tooltip("Duracao do slow motion em segundos (tempo real).")]
        public float slowMotionDurationSeconds = 0.12f;

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
            spawnDelaySeconds = Mathf.Max(0f, spawnDelaySeconds);
            aiControlledDurationSeconds = Mathf.Max(0f, aiControlledDurationSeconds);
            slowMotionTimeScale = Mathf.Clamp(slowMotionTimeScale, MinSlowMotionTimeScale, MaxSlowMotionTimeScale);
            slowMotionDurationSeconds = Mathf.Max(0f, slowMotionDurationSeconds);
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
