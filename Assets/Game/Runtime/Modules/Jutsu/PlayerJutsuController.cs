// Purpose: Handles player jutsu casts from ScriptableObject definitions and consumes chakra bars per slot rules.
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

        [Header("Jutsu Loadout (ScriptableObjects)")]
        public JutsuDefinition slot1Jutsu;
        public JutsuDefinition slot2Jutsu;
        public JutsuDefinition slot3Jutsu;

        private PlayerChakraGauge _chakraGauge;
        private UnitSettings _unitSettings;
        private HealthSystem _healthSystem;
        private StateMachine _stateMachine;
        private UnitActions _unitActions;

        private void Awake() {
            _chakraGauge = GetComponent<PlayerChakraGauge>();
            _unitSettings = GetComponent<UnitSettings>();
            _healthSystem = GetComponent<HealthSystem>();
            _stateMachine = GetComponent<StateMachine>();
            _unitActions = GetComponent<UnitActions>();
        }

        private void Start() {
            _chakraGauge?.ResetChakraToEmpty();
        }

        private void OnEnable() {
            UnitActions.onUnitDealDamage += HandleUnitDealDamage;
        }

        private void OnDisable() {
            UnitActions.onUnitDealDamage -= HandleUnitDealDamage;
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

            if(jutsu.switchPlayerToAiControlled && _stateMachine != null) {
                GameObject closestEnemy = UnitTargetingService.FindClosestHostile(_unitActions);
                if(closestEnemy != null) {
                    _stateMachine.SetControledByAiState(closestEnemy.transform, AiStateStoppingDistance);
                } else {
                    _stateMachine.SetControledByAiState(transform.position, AiStateStoppingDistance);
                }
            }

            if(_unitActions != null && _unitActions.animator != null && !string.IsNullOrWhiteSpace(jutsu.playerAnimation)) {
                _unitActions.animator.Play(jutsu.playerAnimation);
            }

            SpawnJutsuObjects(jutsu);
            string jutsuDisplayName = string.IsNullOrWhiteSpace(jutsu.jutsuName) ? jutsu.name : jutsu.jutsuName;
            Debug.Log($"Jutsu used: {jutsuDisplayName}", this);
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
