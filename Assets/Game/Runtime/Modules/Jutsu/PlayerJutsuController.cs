// Purpose: Handles player jutsu casts from ScriptableObject definitions and consumes chakra bars per slot rules.
using System.Collections;
using UnityEngine;
using WOR.Gameplay.Modules.Units.Services;

namespace WOR.Gameplay.Modules.Jutsu {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitSettings))]
    [RequireComponent(typeof(PlayerChakraGauge))]
    public class PlayerJutsuController : MonoBehaviour {

        private const int SlotOneBarCost = 1;
        private const int SlotTwoBarCost = 1;
        private const int SlotThreeBarCost = 3;
        private const float ForwardSpawnDistance = 1.2f;
        private const float AroundSpawnRadius = 0.9f;
        private const float AiStateStoppingDistance = 0.15f;
        private const float DefaultForcedAnimationDuration = 0.35f;
        private const float MinSlowMotionTimeScale = 0.01f;
        private const float MaxSlowMotionTimeScale = 1f;
        private const float TimeScaleCompareTolerance = 0.001f;

        [Header("Jutsu Loadout (ScriptableObjects)")]
        public JutsuDefinition slot1Jutsu;
        public JutsuDefinition slot2Jutsu;
        public JutsuDefinition slot3Jutsu;

        [Header("Debug")]
        [Tooltip("Quando ativo, inicia a partida com chakra cheio para testes de jutsu.")]
        public bool startWithFullChakra = true;

        private PlayerChakraGauge _chakraGauge;
        private UnitSettings _unitSettings;
        private HealthSystem _healthSystem;
        private StateMachine _stateMachine;
        private UnitActions _unitActions;
        private int _aiControlCastVersion;
        private Coroutine _slowMotionRoutine;
        private bool _slowMotionActive;
        private float _slowMotionRestoreTimeScale = 1f;
        private float _slowMotionRestoreFixedDelta = 0.02f;
        private float _slowMotionAppliedTimeScale = 1f;

        private void Awake() {
            _chakraGauge = GetComponent<PlayerChakraGauge>();
            _unitSettings = GetComponent<UnitSettings>();
            _healthSystem = GetComponent<HealthSystem>();
            _stateMachine = GetComponent<StateMachine>();
            _unitActions = GetComponent<UnitActions>();
        }

        private void Start() {
            if(_chakraGauge == null) {
                return;
            }

            if(startWithFullChakra) {
                _chakraGauge.FillChakraToMax();
            } else {
                _chakraGauge.ResetChakraToEmpty();
            }
        }

        private void OnEnable() {
            UnitActions.onUnitDealDamage += HandleUnitDealDamage;
        }

        private void OnDisable() {
            UnitActions.onUnitDealDamage -= HandleUnitDealDamage;
            StopSlowMotionIfActive();
        }

        private void Update() {
            if(!CanReadInput()) {
                return;
            }

            int playerId = _unitSettings != null ? _unitSettings.playerId : 1;
            if(InputManager.JutsuSlot1KeyDown(playerId)) {
                TryCastJutsu(slot1Jutsu, SlotOneBarCost);
            }
            if(InputManager.JutsuSlot2KeyDown(playerId)) {
                TryCastJutsu(slot2Jutsu, SlotTwoBarCost);
            }
            if(InputManager.JutsuSlot3KeyDown(playerId)) {
                TryCastJutsu(slot3Jutsu, SlotThreeBarCost);
            }
        }

        private bool CanReadInput() {
            if(_unitSettings == null || _unitSettings.unitType != UNITTYPE.PLAYER) {
                return false;
            }

            if(_healthSystem != null && _healthSystem.isDead) {
                return false;
            }

            return true;
        }

        private void HandleUnitDealDamage(GameObject recipient, AttackData attackData) {
            if(_chakraGauge == null || attackData == null || attackData.inflictor != gameObject || recipient == null) {
                return;
            }

            UnitSettings recipientSettings = recipient.GetComponent<UnitSettings>();
            if(recipientSettings == null || recipientSettings.unitType != UNITTYPE.ENEMY) {
                return;
            }

            _chakraGauge.AddHitCharge();
        }

        private void TryCastJutsu(JutsuDefinition jutsu, int chakraBarCost) {
            if(jutsu == null || _chakraGauge == null) {
                return;
            }

            if(!_chakraGauge.TryConsumeBars(chakraBarCost)) {
                return;
            }

            TryApplyJutsuSlowMotion(jutsu);
            ControledByAI aiState = TrySwitchToAiControl(jutsu);
            PlayConfiguredJutsuAnimation(jutsu, aiState);
            SpawnJutsuObjectsWithDelay(jutsu);
            string jutsuDisplayName = string.IsNullOrWhiteSpace(jutsu.jutsuName) ? jutsu.name : jutsu.jutsuName;
            Debug.Log($"Jutsu used: {jutsuDisplayName}", this);
        }

        private ControledByAI TrySwitchToAiControl(JutsuDefinition jutsu) {
            if(jutsu == null || !jutsu.switchPlayerToAiControlled || _stateMachine == null) {
                return null;
            }

            _aiControlCastVersion++;
            int castVersion = _aiControlCastVersion;

            ControledByAI aiState;
            GameObject closestEnemy = UnitTargetingService.FindClosestHostile(_unitActions);
            if(closestEnemy != null) {
                aiState = _stateMachine.SetControledByAiState(closestEnemy.transform, AiStateStoppingDistance);
            } else {
                // Keep AI-controlled state even when no enemy exists yet.
                aiState = _stateMachine.SetControledByAiState();
            }

            ScheduleReturnToIdleFromAiControl(jutsu, castVersion);
            return aiState;
        }

        private void PlayConfiguredJutsuAnimation(JutsuDefinition jutsu, ControledByAI aiState) {
            if(jutsu == null || _unitActions == null || _unitActions.animator == null) {
                return;
            }

            string animationName = jutsu.playerAnimation;
            if(string.IsNullOrWhiteSpace(animationName)) {
                return;
            }

            if(aiState != null) {
                float duration = ResolveAnimationDuration(animationName);
                aiState.PlayForcedAnimation(animationName, duration, true);
                return;
            }

            _unitActions.animator.Play(animationName, 0, 0f);
        }

        private float ResolveAnimationDuration(string animationName) {
            if(_unitActions == null || _unitActions.animator == null || string.IsNullOrWhiteSpace(animationName)) {
                return DefaultForcedAnimationDuration;
            }

            RuntimeAnimatorController runtimeController = _unitActions.animator.runtimeAnimatorController;
            if(runtimeController == null || runtimeController.animationClips == null) {
                return DefaultForcedAnimationDuration;
            }

            AnimationClip[] clips = runtimeController.animationClips;
            for(int i = 0; i < clips.Length; i++) {
                AnimationClip clip = clips[i];
                if(clip == null || clip.name != animationName) {
                    continue;
                }

                return Mathf.Max(0.01f, clip.length);
            }

            return DefaultForcedAnimationDuration;
        }

        private void SpawnJutsuObjectsWithDelay(JutsuDefinition jutsu) {
            if(jutsu == null) {
                return;
            }

            float spawnDelay = Mathf.Max(0f, jutsu.spawnDelaySeconds);
            if(spawnDelay <= 0f) {
                SpawnJutsuObjects(jutsu);
                return;
            }

            StartCoroutine(SpawnJutsuObjectsAfterDelayRoutine(jutsu, spawnDelay));
        }

        private IEnumerator SpawnJutsuObjectsAfterDelayRoutine(JutsuDefinition jutsu, float delaySeconds) {
            if(delaySeconds > 0f) {
                yield return new WaitForSeconds(delaySeconds);
            }

            if(jutsu == null || this == null || !gameObject.activeInHierarchy) {
                yield break;
            }

            SpawnJutsuObjects(jutsu);
        }

        private void ScheduleReturnToIdleFromAiControl(JutsuDefinition jutsu, int castVersion) {
            if(jutsu == null) {
                return;
            }

            float aiDuration = Mathf.Max(0f, jutsu.aiControlledDurationSeconds);
            if(aiDuration <= 0f) {
                return;
            }

            StartCoroutine(ReturnToIdleAfterAiControlledDurationRoutine(aiDuration, castVersion));
        }

        private IEnumerator ReturnToIdleAfterAiControlledDurationRoutine(float duration, int castVersion) {
            yield return new WaitForSeconds(duration);

            if(castVersion != _aiControlCastVersion || _stateMachine == null) {
                yield break;
            }

            if(_healthSystem != null && _healthSystem.isDead) {
                yield break;
            }

            ControledByAI aiState;
            if(_stateMachine.TryGetControledByAiState(out aiState)) {
                _stateMachine.SetState(new PlayerIdle());
            }
        }

        private void TryApplyJutsuSlowMotion(JutsuDefinition jutsu) {
            if(jutsu == null || !jutsu.enableSlowMotion) {
                return;
            }

            float duration = Mathf.Max(0f, jutsu.slowMotionDurationSeconds);
            if(duration <= 0f) {
                return;
            }

            if(Time.timeScale <= 0f) {
                return;
            }

            float targetTimeScale = Mathf.Clamp(jutsu.slowMotionTimeScale, MinSlowMotionTimeScale, MaxSlowMotionTimeScale);
            if(targetTimeScale >= MaxSlowMotionTimeScale) {
                return;
            }

            if(!_slowMotionActive) {
                _slowMotionRestoreTimeScale = Time.timeScale;
                _slowMotionRestoreFixedDelta = Time.fixedDeltaTime;
                _slowMotionActive = true;
            }

            float fixedDeltaAtScaleOne = ResolveFixedDeltaAtScaleOne();
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = fixedDeltaAtScaleOne * targetTimeScale;
            _slowMotionAppliedTimeScale = targetTimeScale;

            if(_slowMotionRoutine != null) {
                StopCoroutine(_slowMotionRoutine);
            }

            _slowMotionRoutine = StartCoroutine(StopSlowMotionAfterDurationRoutine(duration, targetTimeScale));
        }

        private float ResolveFixedDeltaAtScaleOne() {
            if(Time.timeScale > Mathf.Epsilon) {
                return Time.fixedDeltaTime / Time.timeScale;
            }

            if(_slowMotionRestoreTimeScale > Mathf.Epsilon) {
                return _slowMotionRestoreFixedDelta / _slowMotionRestoreTimeScale;
            }

            return 0.02f;
        }

        private IEnumerator StopSlowMotionAfterDurationRoutine(float duration, float appliedTimeScale) {
            yield return new WaitForSecondsRealtime(duration);

            _slowMotionRoutine = null;
            if(!_slowMotionActive) {
                yield break;
            }

            bool timeScaleChangedExternally = Mathf.Abs(Time.timeScale - appliedTimeScale) > TimeScaleCompareTolerance;
            if(!timeScaleChangedExternally) {
                Time.timeScale = _slowMotionRestoreTimeScale;
                Time.fixedDeltaTime = _slowMotionRestoreFixedDelta;
            }

            _slowMotionActive = false;
        }

        private void StopSlowMotionIfActive() {
            if(_slowMotionRoutine != null) {
                StopCoroutine(_slowMotionRoutine);
                _slowMotionRoutine = null;
            }

            if(!_slowMotionActive) {
                return;
            }

            bool canRestoreTimeScale = Mathf.Abs(Time.timeScale - _slowMotionAppliedTimeScale) <= TimeScaleCompareTolerance;
            if(canRestoreTimeScale) {
                Time.timeScale = _slowMotionRestoreTimeScale;
                Time.fixedDeltaTime = _slowMotionRestoreFixedDelta;
            }

            _slowMotionActive = false;
        }

        private void SpawnJutsuObjects(JutsuDefinition jutsu) {
            if(jutsu == null || jutsu.spawnPrefabs == null || jutsu.spawnPrefabs.Count == 0) {
                return;
            }

            for(int i = 0; i < jutsu.spawnPrefabs.Count; i++) {
                GameObject prefab = jutsu.spawnPrefabs[i];
                if(prefab == null) {
                    continue;
                }

                Vector3 spawnPosition = ResolveSpawnPosition(jutsu.spawnPosition);
                GameObject spawnedObject = Instantiate(prefab, spawnPosition, transform.rotation);
                InitializeSpawnedJutsuObject(spawnedObject, jutsu);
            }
        }

        private void InitializeSpawnedJutsuObject(GameObject spawnedObject, JutsuDefinition jutsu) {
            if(spawnedObject == null) {
                return;
            }

            JutsuDamageHitbox[] hitboxes = spawnedObject.GetComponentsInChildren<JutsuDamageHitbox>(true);
            for(int i = 0; i < hitboxes.Length; i++) {
                if(hitboxes[i] != null) {
                    hitboxes[i].Initialize(gameObject, jutsu);
                }
            }

            Projectile[] projectiles = spawnedObject.GetComponentsInChildren<Projectile>(true);
            for(int i = 0; i < projectiles.Length; i++) {
                Projectile projectile = projectiles[i];
                if(projectile == null) {
                    continue;
                }

                if(jutsu != null) {
                    projectile.attackData = jutsu.BuildAttackData(gameObject);
                    projectile.useInflictorStatsForDamage = jutsu.UsesCasterStatsForDamage;
                } else if(projectile.attackData != null) {
                    projectile.attackData.inflictor = gameObject;
                }
            }
        }

        private Vector3 ResolveSpawnPosition(JutsuSpawnPosition spawnPosition) {
            switch(spawnPosition) {
                case JutsuSpawnPosition.InFrontOfPlayer:
                    return GetForwardSpawnPosition();
                case JutsuSpawnPosition.AroundPlayerSmallRadius:
                    return GetAroundSpawnPosition();
                case JutsuSpawnPosition.ClosestEnemy:
                    return GetEnemyBasedSpawnPosition(true);
                case JutsuSpawnPosition.FarthestEnemy:
                    return GetEnemyBasedSpawnPosition(false);
                default:
                    return transform.position;
            }
        }

        private Vector3 GetForwardSpawnPosition() {
            DIRECTION facingDirection = _unitActions != null ? _unitActions.dir : DIRECTION.RIGHT;
            float signedDistance = (int)facingDirection * ForwardSpawnDistance;
            Vector3 basePosition = transform.position;
            return new Vector3(basePosition.x + signedDistance, basePosition.y, basePosition.z);
        }

        private Vector3 GetAroundSpawnPosition() {
            Vector2 randomOffset = Random.insideUnitCircle * AroundSpawnRadius;
            Vector3 basePosition = transform.position;
            return new Vector3(basePosition.x + randomOffset.x, basePosition.y, basePosition.z + randomOffset.y);
        }

        private Vector3 GetEnemyBasedSpawnPosition(bool closest) {
            if(_unitActions == null) {
                return transform.position;
            }

            UnitActions selectedEnemy = null;
            float bestDistance = closest ? float.MaxValue : float.MinValue;
            Vector3 sourcePosition = transform.position;

            foreach(UnitActions candidate in UnitRegistryService.GetRegisteredUnits()) {
                if(candidate == null || !candidate.isEnemy) {
                    continue;
                }

                if(!UnitTargetingService.IsValidCombatTarget(_unitActions, candidate)) {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - sourcePosition).sqrMagnitude;
                bool shouldReplace = closest ? sqrDistance < bestDistance : sqrDistance > bestDistance;
                if(!shouldReplace) {
                    continue;
                }

                bestDistance = sqrDistance;
                selectedEnemy = candidate;
            }

            return selectedEnemy != null ? selectedEnemy.transform.position : transform.position;
        }
    }
}
