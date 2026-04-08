// Purpose: Applies jutsu damage/effects from trigger hitboxes while respecting caster ownership and factions.
using System.Collections.Generic;
using UnityEngine;
using WOR.Gameplay.Modules.Combat.Services;
using WOR.Gameplay.Modules.Units.Services;

namespace WOR.Gameplay.Modules.Jutsu {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class JutsuDamageHitbox : MonoBehaviour {

        private const int MinDamage = 0;

        [Header("Fallback Combat (Used Without JutsuDefinition)")]
        [Min(MinDamage)] public int damage = 10;
        public bool useInflictorStatsForDamage;
        public AttackData attackData = CreateDefaultAttackData();

        [Header("Hit Behaviour")]
        public bool hitEachTargetOnlyOnce = true;
        [Min(0f)] public float repeatHitInterval;

        [Header("Runtime")]
        [ReadOnlyProperty] public GameObject spawnedBy;

        private readonly Dictionary<int, float> _nextAllowedHitTimeByTarget = new Dictionary<int, float>();
        private AttackData _runtimeAttackData;
        private bool _runtimeUseInflictorStatsForDamage;
        private UnitActions _ownerActions;
        private UnitSettings _ownerSettings;

        private void Awake() {
            EnsureTriggerCollider();
            EnsureAttackDataConfigured();
            CacheOwnerReferences();
        }

        private void OnEnable() {
            _nextAllowedHitTimeByTarget.Clear();
        }

        private void OnValidate() {
            EnsureTriggerCollider();
            EnsureAttackDataConfigured();
            damage = Mathf.Max(MinDamage, damage);
            repeatHitInterval = Mathf.Max(0f, repeatHitInterval);
        }

        public void Initialize(GameObject owner, JutsuDefinition jutsuDefinition = null) {
            spawnedBy = owner;
            CacheOwnerReferences();
            ConfigureRuntimeAttackData(jutsuDefinition);
        }

        private void OnTriggerEnter(Collider other) {
            TryApplyHit(other);
        }

        private void OnTriggerStay(Collider other) {
            TryApplyHit(other);
        }

        private void TryApplyHit(Collider other) {
            if(other == null) {
                return;
            }

            HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
            if(targetHealth == null || targetHealth.isDead) {
                return;
            }

            GameObject targetObject = targetHealth.gameObject;
            if(!CanAffectTarget(targetObject) || !CanHitTargetNow(targetObject)) {
                return;
            }

            EnsureRuntimeAttackDataConfigured();
            if(_runtimeAttackData == null) {
                return;
            }

            UnitSettings targetSettings = targetObject.GetComponent<UnitSettings>();
            int finalDamage = ResolveDamage(targetSettings);

            if(finalDamage > 0) {
                targetHealth.SubstractHealth(finalDamage);
            }

            ApplyElementalCondition(targetObject);

            if(!string.IsNullOrEmpty(_runtimeAttackData.sfx)) {
                AudioController.PlaySFX(_runtimeAttackData.sfx, transform.position);
            }

            ApplyHitReaction(targetObject, targetHealth);
            UnitActions.NotifyExternalDealDamage(targetObject, _runtimeAttackData);
        }

        private void ApplyElementalCondition(GameObject targetObject) {
            if(_runtimeAttackData == null || _runtimeAttackData.conditionType == CONDITIONTYPE.NONE) {
                return;
            }

            float conditionCharge = _runtimeAttackData.GetConditionCharge();
            if(conditionCharge <= 0f) {
                return;
            }

            ConditionManager conditionManager = targetObject.GetComponent<ConditionManager>();
            if(conditionManager == null) {
                return;
            }

            conditionManager.ApplyConditionCharge(
                _runtimeAttackData.conditionType,
                conditionCharge,
                spawnedBy);
        }

        private void ApplyHitReaction(GameObject targetObject, HealthSystem targetHealth) {
            UnitActions targetActions = targetObject.GetComponent<UnitActions>();
            StateMachine targetStateMachine = targetObject.GetComponent<StateMachine>();
            if(targetActions == null) {
                return;
            }

            if(targetHealth != null && targetHealth.isDead) {
                targetStateMachine?.SetState(new UnitDeath(true));
                return;
            }

            bool doKnockdown = _runtimeAttackData != null
                && _runtimeAttackData.knockdown
                && targetActions.settings != null
                && targetActions.settings.canBeKnockedDown;

            if(doKnockdown) {
                float knockdownHorizontalForce = _runtimeAttackData.knockdownLaunchHorizontalForce > 0f
                    ? _runtimeAttackData.knockdownLaunchHorizontalForce
                    : targetActions.settings.knockDownDistance;
                float knockdownVerticalForce = _runtimeAttackData.knockdownLaunchVerticalForce > 0f
                    ? _runtimeAttackData.knockdownLaunchVerticalForce
                    : targetActions.settings.knockDownHeight;

                targetStateMachine?.SetState(new UnitKnockDown(_runtimeAttackData, knockdownHorizontalForce, knockdownVerticalForce));
                return;
            }

            if(targetActions.isGrounded && _runtimeAttackData != null) {
                targetActions.ApplyRegularHitKnockback(_runtimeAttackData, ResolveAttackDirection(targetObject.transform.position.x));
                targetStateMachine?.SetState(new UnitHit());
            }
        }

        private int ResolveDamage(UnitSettings targetSettings) {
            if(_runtimeAttackData == null) {
                return 0;
            }

            if(!_runtimeUseInflictorStatsForDamage) {
                return Mathf.Max(0, _runtimeAttackData.damage);
            }

            return CombatDamageCalculator.CalculateDamageFromInflictor(
                _runtimeAttackData,
                spawnedBy,
                targetSettings,
                out bool _);
        }

        private DIRECTION ResolveAttackDirection(float targetXPosition) {
            if(_ownerActions != null) {
                return _ownerActions.dir;
            }

            float sourceXPosition = spawnedBy != null ? spawnedBy.transform.position.x : transform.position.x;
            return targetXPosition >= sourceXPosition ? DIRECTION.RIGHT : DIRECTION.LEFT;
        }

        private bool CanAffectTarget(GameObject targetObject) {
            if(targetObject == null) {
                return false;
            }

            if(IsOwnerOrOwnerHierarchy(targetObject)) {
                return false;
            }

            UnitActions targetActions = targetObject.GetComponent<UnitActions>();
            if(_ownerActions != null && targetActions != null) {
                return UnitTargetingService.IsValidCombatTarget(_ownerActions, targetActions);
            }

            UnitSettings targetSettings = targetObject.GetComponent<UnitSettings>();
            if(_ownerSettings != null && targetSettings != null) {
                bool sameFaction = _ownerSettings.faction == targetSettings.faction;
                bool ownerConfused = _ownerSettings.faction == UNITFACTION.CONFUSED;
                if(sameFaction && !ownerConfused) {
                    return false;
                }
            }

            HealthSystem targetHealth = targetObject.GetComponent<HealthSystem>();
            if(targetHealth != null && targetHealth.isDead) {
                return false;
            }

            return true;
        }

        private bool CanHitTargetNow(GameObject targetObject) {
            int targetId = targetObject.GetInstanceID();
            if(!_nextAllowedHitTimeByTarget.TryGetValue(targetId, out float nextAllowedHitTime)) {
                _nextAllowedHitTimeByTarget[targetId] = GetNextAllowedHitTime();
                return true;
            }

            if(hitEachTargetOnlyOnce) {
                return false;
            }

            if(Time.time < nextAllowedHitTime) {
                return false;
            }

            _nextAllowedHitTimeByTarget[targetId] = GetNextAllowedHitTime();
            return true;
        }

        private float GetNextAllowedHitTime() {
            if(hitEachTargetOnlyOnce) {
                return float.PositiveInfinity;
            }

            return Time.time + repeatHitInterval;
        }

        private bool IsOwnerOrOwnerHierarchy(GameObject targetObject) {
            if(spawnedBy == null || targetObject == null) {
                return false;
            }

            Transform ownerTransform = spawnedBy.transform;
            Transform targetTransform = targetObject.transform;

            if(targetObject == spawnedBy) {
                return true;
            }

            return targetTransform.IsChildOf(ownerTransform) || ownerTransform.IsChildOf(targetTransform);
        }

        private void CacheOwnerReferences() {
            _ownerActions = spawnedBy != null ? spawnedBy.GetComponent<UnitActions>() : null;
            _ownerSettings = spawnedBy != null ? spawnedBy.GetComponent<UnitSettings>() : null;
        }

        private void EnsureRuntimeAttackDataConfigured() {
            if(_runtimeAttackData != null) {
                return;
            }

            ConfigureRuntimeAttackData(null);
        }

        private void ConfigureRuntimeAttackData(JutsuDefinition jutsuDefinition) {
            if(jutsuDefinition != null) {
                _runtimeAttackData = jutsuDefinition.BuildAttackData(spawnedBy);
                _runtimeUseInflictorStatsForDamage = jutsuDefinition.UsesCasterStatsForDamage;
                return;
            }

            _runtimeAttackData = CloneAttackData(attackData);
            _runtimeAttackData.damage = Mathf.Max(MinDamage, damage);
            _runtimeAttackData.inflictor = spawnedBy;
            _runtimeUseInflictorStatsForDamage = useInflictorStatsForDamage;

            if(!_runtimeUseInflictorStatsForDamage) {
                _runtimeAttackData.strengthDamageScale = 0f;
            }
        }

        private void EnsureAttackDataConfigured() {
            if(attackData == null) {
                attackData = CreateDefaultAttackData();
            }

            attackData.damage = Mathf.Max(MinDamage, damage);
        }

        private void EnsureTriggerCollider() {
            Collider hitCollider = GetComponent<Collider>();
            if(hitCollider == null) {
                return;
            }

            hitCollider.isTrigger = true;
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
            return new AttackData("Jutsu Hitbox", 10, null, ATTACKTYPE.PUNCH, false);
        }
    }
}
