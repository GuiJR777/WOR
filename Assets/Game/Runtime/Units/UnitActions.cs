// Purpose: Provides reusable runtime actions for units (movement, combat, sensing and effects) in 2.5D.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using WOR.Gameplay.Modules.Combat.Services;
using WOR.Gameplay.Modules.Units.Services;

namespace WOR.Gameplay {

    public enum DEFENSERESULT {
        NONE = 0,
        BLOCKED = 1,
        PARRIED = 2,
    }

    public class UnitActions : MonoBehaviour {

        private const float INPUT_DEADZONE = 0.05f;
        private const float DASH_INPUT_THRESHOLD = 0.75f;
        private const float DOUBLE_TAP_WINDOW = 0.25f;
        private const float FOOTSTEP_OVERLAP_RADIUS = 0.2f;
        private const float DEFAULT_WALL_CHECK_DISTANCE = 0.35f;
        private const float DEFAULT_KNOCKBACK_DURATION = 0.1f;
        private const float DEFAULT_ATTACK_ADVANCE_DURATION = 0.08f;
        private const float DEFAULT_GHOST_LIFETIME = 0.18f;
        private const int GHOST_POOL_DEFAULT_CAPACITY = 8;
        private const int GHOST_POOL_MAX_SIZE = 48;
        private const float DEFAULT_CAPSULE_RADIUS = 0.3f;
        private const float DEFAULT_CAPSULE_HEIGHT = 0.6f;
        private const float CAPSULE_CAST_SKIN = 0.02f;
        private const float GROUNDED_CHECK_DISTANCE = 0.08f;
        private const float GROUND_NORMAL_THRESHOLD = 0.5f;
        private const float GROUND_STICK_MAX_UPWARD = 0.05f;
        private const float MIN_JUMP_VELOCITY = 0.5f;
        private const float AUTO_GRAB_DIRECTION_THRESHOLD = 0.2f;
        private const float AUTO_GRAB_DEPTH_RANGE = 0.7f;
        private const float DEFAULT_STEP_HEIGHT = 0.45f;

        [HideInInspector] public GameObject target;
        [HideInInspector] public float groundPos;
        [HideInInspector] public float baseHeight;
        [HideInInspector] public Vector2 currentPosition => new Vector2(transform.position.x, groundPos);
        [HideInInspector] public float lastAttackTime;
        [HideInInspector] public ATTACKTYPE lastAttackType;
        [HideInInspector] public float yForce;
        [HideInInspector] public bool isGrounded = true;
        [HideInInspector] public WeaponPickup weapon;
        [HideInInspector] public bool targetSpotted;
        [HideInInspector] public List<ATTACKTYPE> attackList = new List<ATTACKTYPE>();

        public Animator animator => GetComponent<Animator>();
        public StateMachine stateMachine => GetComponent<StateMachine>();
        public UnitSettings settings => GetComponent<UnitSettings>();
        public bool isPlayer => settings != null && settings.unitType == UNITTYPE.PLAYER;
        public bool isEnemy => settings != null && settings.unitType == UNITTYPE.ENEMY;
        public DIRECTION dir {
            get {
                float yRotation = Mathf.Repeat(transform.localEulerAngles.y, 360f);
                bool facingLeft = yRotation > 90f && yRotation < 270f;
                return facingLeft ? DIRECTION.LEFT : DIRECTION.RIGHT;
            }
        }
        public DIRECTION invertedDir => (DIRECTION)((int)dir * -1);
        public bool IsDashAvailable => settings != null && settings.canDash && Time.time - _lastDashTime >= settings.dashCooldown;

        public delegate void OnUnitDealDamage(GameObject recipient, AttackData attackData);
        public static event OnUnitDealDamage onUnitDealDamage;

        private sealed class GhostFrame {
            public GameObject gameObject;
            public SpriteRenderer renderer;
            public float releaseTime;
        }

        private SpriteRenderer _spriteRenderer;
        private bool _onApplicationQuit;
        private float _currentSpeed;
        private float _animDuration;
        private Vector3 _lastGroundMoveDirection = Vector3.right;
        private bool _dashInputWasPressed;
        private float _lastHorizontalTapSign;
        private float _lastHorizontalTapTime;
        private float _lastDashTime = -999f;
        private bool _knockbackActive;
        private Vector3 _knockbackVelocity;
        private float _knockbackEndTime;
        private bool _attackAdvanceActive;
        private Vector3 _attackAdvanceVelocity;
        private float _attackAdvanceEndTime;
        private Coroutine _ghostTrailRoutine;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        private ObjectPool<GhostFrame> _ghostFramePool;
        private readonly List<GhostFrame> _activeGhostFrames = new List<GhostFrame>();
        private GameObject _touchedEnemyCandidate;

        private void Awake() {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            Ensure3DUnitPhysicsSetup();

            Vector3 currentPosition = GetUnitPosition();
            groundPos = currentPosition.z;
            baseHeight = currentPosition.y;
        }

        private void OnEnable() {
            UnitRegistryService.Register(this);
        }

        private void OnDisable() {
            UnitRegistryService.Unregister(this);
        }

        private void Ensure3DUnitPhysicsSetup() {
            EnsureRigidbody3D();
            EnsureCapsuleCollider3D();
            DisableLegacy2DPhysicsComponents();
        }

        private void EnsureRigidbody3D() {
            Rigidbody body = GetComponent<Rigidbody>();
            if(body == null) {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody = body;
        }

        private int GetGroundPhysicsMask() {
            int mask = LayerMask.GetMask("Environment", "Surface", "Default");
            return mask != 0 ? mask : Physics.DefaultRaycastLayers;
        }

        private void EnsureCapsuleCollider3D() {
            CapsuleCollider collider3D = GetComponent<CapsuleCollider>();
            CapsuleCollider2D collider2D = GetComponent<CapsuleCollider2D>();

            if(collider3D == null) {
                collider3D = gameObject.AddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider3D(collider3D, collider2D);
            }

            if(collider3D != null && collider3D.radius <= 0f) {
                collider3D.radius = DEFAULT_CAPSULE_RADIUS;
                collider3D.height = DEFAULT_CAPSULE_HEIGHT;
                collider3D.direction = 1;
            }

            _capsuleCollider = collider3D;
        }

        private static void ConfigureCapsuleCollider3D(CapsuleCollider collider3D, CapsuleCollider2D collider2D) {
            collider3D.center = Vector3.zero;
            collider3D.radius = DEFAULT_CAPSULE_RADIUS;
            collider3D.height = DEFAULT_CAPSULE_HEIGHT;
            collider3D.direction = 1;
            collider3D.isTrigger = collider2D != null && collider2D.isTrigger;
        }

        private void DisableLegacy2DPhysicsComponents() {
            Rigidbody2D body2D = GetComponent<Rigidbody2D>();
            if(body2D != null) {
                body2D.bodyType = RigidbodyType2D.Kinematic;
                body2D.simulated = false;
            }

            CapsuleCollider2D collider2D = GetComponent<CapsuleCollider2D>();
            if(collider2D != null) {
                collider2D.enabled = false;
            }
        }

        private void OnDestroy() {
            if(settings != null && settings.shadow != null && !_onApplicationQuit) {
                Destroy(settings.shadow);
            }

            CleanupGhostPool();
        }

        // SECTION: TARGETING, DIRECTION AND COMBAT

        public GameObject findClosestPlayer() {
            return UnitTargetingService.FindClosestHostile(this);
        }

        public bool IsValidTargetForCombat(GameObject candidate) {
            UnitActions candidateActions = candidate != null ? candidate.GetComponent<UnitActions>() : null;
            return IsValidTargetForCombat(candidateActions);
        }

        private bool IsValidTargetForCombat(UnitActions candidateActions) {
            return UnitTargetingService.IsValidCombatTarget(this, candidateActions);
        }

        public Vector2 distanceToTarget() {
            if(target == null) {
                return Vector2.positiveInfinity;
            }

            float depth = GetGroundDepth(target);
            return new Vector2(
                Mathf.Abs(target.transform.position.x - transform.position.x),
                Mathf.Abs(depth - groundPos)
            );
        }

        public void TurnToTarget() {
            if(target == null) {
                return;
            }
            transform.localRotation = target.transform.position.x < transform.position.x
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }

        public void TurnToDir(DIRECTION lookDirection) {
            transform.localRotation = lookDirection == DIRECTION.LEFT
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }

        public void TurnToFloatDir(float x) {
            if(Mathf.Abs(x) <= INPUT_DEADZONE) {
                return;
            }
            TurnToDir(x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT);
        }

        public bool CheckForHit(AttackData attackData) {
            if(attackData == null || !HitBoxActive()) {
                return false;
            }

            bool damageDealt = false;
            if(attackData.inflictor == null) {
                attackData.inflictor = gameObject;
            }

            List<GameObject> objectsHit = GetObjectsHit(attackData);
            for(int i = 0; i < objectsHit.Count; i++) {
                GameObject obj = objectsHit[i];
                if(obj == null) {
                    continue;
                }

                UnitActions targetUnit = obj.GetComponent<UnitActions>();
                DEFENSERESULT defenseResult = targetUnit != null
                    ? targetUnit.ResolveIncomingAttack(invertedDir, this, attackData)
                    : DEFENSERESULT.NONE;

                if(defenseResult == DEFENSERESULT.BLOCKED || defenseResult == DEFENSERESULT.PARRIED) {
                    damageDealt = true;
                    continue;
                }

                bool unitKnockdownInProgress = obj.GetComponent<StateMachine>()?.GetCurrentState() is UnitKnockDown;
                if(unitKnockdownInProgress) {
                    continue;
                }

                ShowHitEffectAtPosition(settings != null && settings.hitBox != null
                    ? settings.hitBox.transform.position + (Vector3.right * Random.Range(0f, 0.5f))
                    : transform.position);

                HealthSystem targetHealthSystem = obj.GetComponent<HealthSystem>();
                if(targetHealthSystem != null) {
                    int finalDamage = CombatDamageCalculator.CalculateDamage(
                        attackData,
                        settings,
                        targetUnit != null ? targetUnit.settings : null,
                        out bool _);

                    if(finalDamage > 0) {
                        targetHealthSystem.SubstractHealth(finalDamage);
                    }
                }

                if(!string.IsNullOrEmpty(attackData.sfx)) {
                    AudioController.PlaySFX(attackData.sfx);
                }

                onUnitDealDamage?.Invoke(obj, attackData);

                if(targetUnit != null) {
                    if(targetHealthSystem != null && targetHealthSystem.isDead) {
                        obj.GetComponent<StateMachine>()?.SetState(new UnitDeath(true));
                    } else {
                        bool doKnockdown = attackData.knockdown && targetUnit.settings != null && targetUnit.settings.canBeKnockedDown;

                        if(doKnockdown) {
                            float knockdownHorizontalForce = attackData.knockdownLaunchHorizontalForce > 0f
                                ? attackData.knockdownLaunchHorizontalForce
                                : targetUnit.settings.knockDownDistance;
                            float knockdownVerticalForce = attackData.knockdownLaunchVerticalForce > 0f
                                ? attackData.knockdownLaunchVerticalForce
                                : targetUnit.settings.knockDownHeight;
                            Vector2 knockDownForce = new Vector2(knockdownHorizontalForce, knockdownVerticalForce);
                            targetUnit.stateMachine?.SetState(new UnitKnockDown(attackData, knockDownForce.x, knockDownForce.y));
                        } else if(targetUnit.isGrounded) {
                            targetUnit.ApplyRegularHitKnockback(attackData, dir);
                            targetUnit.stateMachine?.SetState(new UnitHit());
                        }
                    }
                }
                damageDealt = true;
            }
            return damageDealt;
        }

        public DEFENSERESULT ResolveIncomingAttack(DIRECTION attackDir, UnitActions attacker, AttackData attackData) {
            if(isEnemy && settings != null && settings.defendChance > 0f && !(stateMachine.GetCurrentState() is UnitDefend)) {
                if(Random.Range(0f, 100f) < settings.defendChance) {
                    stateMachine.SetState(new UnitDefend());
                }
            }

            UnitDefend defendState = stateMachine.GetCurrentState() as UnitDefend;
            if(defendState == null) {
                return DEFENSERESULT.NONE;
            }

            bool canDefendFromThisDirection = settings != null && (settings.rearDefenseEnabled || dir == attackDir);
            if(!canDefendFromThisDirection) {
                return DEFENSERESULT.NONE;
            }

            if(defendState.TryParry(attacker, attackData)) {
                return DEFENSERESULT.PARRIED;
            }

            defendState.Hit();
            return DEFENSERESULT.BLOCKED;
        }

        public void OnParried(UnitActions defender) {
            if(defender == null || settings == null) {
                return;
            }

            float force = Mathf.Max(0f, defender.settings != null ? defender.settings.parryKnockbackForce : settings.hitKnockbackForce);
            float duration = Mathf.Max(
                DEFAULT_KNOCKBACK_DURATION,
                defender.settings != null ? defender.settings.parryKnockbackDuration : settings.hitKnockbackDuration);

            ApplyGroundKnockback(defender.dir, force, duration);

            float stunDuration = defender.settings != null ? defender.settings.parryStunDuration : 0.35f;
            stateMachine?.SetState(new UnitStunned(stunDuration));
        }

        public void ApplyRegularHitKnockback(AttackData attackData, DIRECTION attackerDirection) {
            if(settings == null || attackData == null || !attackData.applyKnockback) {
                return;
            }

            float force = attackData.knockbackForce > 0f ? attackData.knockbackForce : settings.hitKnockbackForce;
            float duration = attackData.knockbackDuration > 0f ? attackData.knockbackDuration : settings.hitKnockbackDuration;
            ApplyGroundKnockback(attackerDirection, force, duration);
            ApplyVerticalKnockback(attackData.knockbackVerticalForce);
        }

        public void ApplyGroundKnockback(DIRECTION moveDirection, float force, float duration) {
            if(force <= 0f || duration <= 0f) {
                return;
            }

            float speed = force / duration;
            _knockbackVelocity = new Vector3((int)moveDirection * speed, 0f, 0f);
            _knockbackEndTime = Time.time + duration;
            _knockbackActive = true;
        }

        public void ApplyAttackForwardMovement(AttackData attackData) {
            if(attackData == null) {
                return;
            }

            float forwardDistance = Mathf.Max(0f, attackData.attackerForwardDistance);
            if(forwardDistance <= 0f) {
                return;
            }

            float duration = attackData.attackerForwardDuration > 0f
                ? attackData.attackerForwardDuration
                : DEFAULT_ATTACK_ADVANCE_DURATION;

            float clampedDuration = Mathf.Max(0.01f, duration);
            float speed = forwardDistance / clampedDuration;
            _attackAdvanceVelocity = new Vector3((int)dir * speed, 0f, 0f);
            _attackAdvanceEndTime = Time.time + clampedDuration;
            _attackAdvanceActive = true;
        }

        private void ApplyVerticalKnockback(float verticalForce) {
            if(Mathf.Abs(verticalForce) <= INPUT_DEADZONE) {
                return;
            }

            Vector3 velocity = GetLinearVelocity();
            velocity.y = verticalForce;
            SetLinearVelocity(velocity);
            yForce = velocity.y;

            if(verticalForce > 0f) {
                isGrounded = false;
            }
        }

        public void TickExternalForces() {
            TickGhostFrames();
            TickGroundingAndGravity();

            State currentState = stateMachine != null ? stateMachine.GetCurrentState() : null;
            if(currentState is UnitKnockDown) {
                return;
            }

            if(_knockbackActive) {
                if(Time.time >= _knockbackEndTime) {
                    _knockbackActive = false;
                    _knockbackVelocity = Vector3.zero;
                } else {
                    Vector3 knockbackVelocity = GetLinearVelocity();
                    knockbackVelocity.x = _knockbackVelocity.x;
                    knockbackVelocity.z = _knockbackVelocity.z;
                    SetLinearVelocity(knockbackVelocity);
                    return;
                }
            }

            if(!_attackAdvanceActive) {
                return;
            }

            if(Time.time >= _attackAdvanceEndTime) {
                _attackAdvanceActive = false;
                _attackAdvanceVelocity = Vector3.zero;
                return;
            }

            Vector3 attackAdvanceVelocity = GetLinearVelocity();
            attackAdvanceVelocity.x = _attackAdvanceVelocity.x;
            attackAdvanceVelocity.z = _attackAdvanceVelocity.z;
            if(isGrounded && attackAdvanceVelocity.y < 0f) {
                attackAdvanceVelocity.y = 0f;
            }
            SetLinearVelocity(attackAdvanceVelocity);
        }

        public List<GameObject> GetObjectsHit(AttackData attackData) {
            List<GameObject> hittableObjects = new List<GameObject>();
            List<GameObject> objectsHit = new List<GameObject>();

            if(isPlayer) {
                AppendObjectsWithTag(hittableObjects, "Enemy");
                AppendObjectsWithTag(hittableObjects, "Object");
            }

            if(isEnemy) {
                bool enemyIsBeingThrown = attackData.attackType == ATTACKTYPE.GRABTHROW;
                bool enemyDoesFallDamage = settings != null && settings.hitOtherEnemiesWhenFalling;

                if(!enemyIsBeingThrown) {
                    AppendObjectsWithTag(hittableObjects, "Player");
                }

                if(enemyIsBeingThrown || enemyDoesFallDamage) {
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                    for(int i = 0; i < enemies.Length; i++) {
                        GameObject enemy = enemies[i];
                        if(enemy == null || enemy == gameObject) {
                            continue;
                        }

                        StateMachine state = enemy.GetComponent<StateMachine>();
                        bool enemyIsKnockedDown = state != null && (state.GetCurrentState() is UnitKnockDown || state.GetCurrentState() is UnitKnockDownGrounded);
                        if(!enemyIsKnockedDown) {
                            hittableObjects.Add(enemy);
                        }
                    }
                }
            }

            for(int i = hittableObjects.Count - 1; i >= 0; i--) {
                GameObject candidate = hittableObjects[i];
                if(candidate == null) {
                    hittableObjects.RemoveAt(i);
                    continue;
                }

                HealthSystem healthSystem = candidate.GetComponent<HealthSystem>();
                if(healthSystem != null && healthSystem.isDead) {
                    hittableObjects.RemoveAt(i);
                    continue;
                }

                StateMachine candidateStateMachine = candidate.GetComponent<StateMachine>();
                if(candidateStateMachine != null && candidateStateMachine.GetCurrentState() is UnitHit) {
                    hittableObjects.RemoveAt(i);
                }
            }

            SortByDistance(hittableObjects);

            for(int i = 0; i < hittableObjects.Count; i++) {
                GameObject candidate = hittableObjects[i];
                SpriteRenderer candidateSpriteRenderer = candidate.GetComponent<SpriteRenderer>();
                if(candidateSpriteRenderer == null || settings == null || settings.hitBox == null) {
                    continue;
                }

                bool hitboxIntersects = settings.hitBox.bounds.Intersects(candidateSpriteRenderer.bounds);
                if(hitboxIntersects && targetInZRange(candidate, 0.5f)) {
                    objectsHit.Add(candidateSpriteRenderer.gameObject);
                }
            }
            return objectsHit;
        }

        public GameObject GetClosestPickup(Vector2 pickupRange) {
            GameObject[] allPickups = GameObject.FindGameObjectsWithTag("Pickup");
            float closestDistance = float.MaxValue;
            GameObject closestPickup = null;

            for(int i = 0; i < allPickups.Length; i++) {
                GameObject pickup = allPickups[i];
                if(pickup == null) {
                    continue;
                }

                float xDistance = Mathf.Abs(transform.position.x - pickup.transform.position.x);
                float depthDistance = Mathf.Abs(groundPos - GetGroundDepth(pickup));
                float maxDistance = pickupRange.magnitude;
                float distance = Mathf.Sqrt((xDistance * xDistance) + (depthDistance * depthDistance));

                if(distance <= maxDistance && distance < closestDistance) {
                    closestDistance = distance;
                    closestPickup = pickup;
                }
            }
            return closestPickup;
        }

        public bool TryAutoGrabEnemyFromStep(Vector2 moveInput, out GameObject enemyToGrab) {
            enemyToGrab = null;
            if(!isPlayer || weapon != null || stateMachine == null || settings == null || !isGrounded) {
                _touchedEnemyCandidate = null;
                return false;
            }

            if(moveInput.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                return false;
            }

            GameObject touchingEnemy = GetTouchingEnemyCandidate();
            if(touchingEnemy == null) {
                _touchedEnemyCandidate = null;
                return false;
            }

            bool touchingSameEnemyAsPreviousStep = touchingEnemy == _touchedEnemyCandidate;
            _touchedEnemyCandidate = touchingEnemy;
            if(!touchingSameEnemyAsPreviousStep) {
                return false;
            }

            UnitActions enemyActions = touchingEnemy.GetComponent<UnitActions>();
            float enemyDepth = enemyActions != null ? enemyActions.groundPos : touchingEnemy.transform.position.z;
            Vector2 toEnemy = new Vector2(
                touchingEnemy.transform.position.x - transform.position.x,
                enemyDepth - groundPos);

            if(toEnemy.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                toEnemy = new Vector2((int)dir, 0f);
            }

            Vector2 moveDirection = moveInput.normalized;
            float moveTowardEnemy = Vector2.Dot(moveDirection, toEnemy.normalized);
            if(moveTowardEnemy < AUTO_GRAB_DIRECTION_THRESHOLD) {
                return false;
            }

            enemyToGrab = touchingEnemy;
            _touchedEnemyCandidate = null;
            return true;
        }

        public GameObject NearbyEnemyDown() {
            const float range = 1f;
            return UnitTargetingService.FindNearbyDownedHostile(this, range);
        }

        public bool targetInZRange(GameObject targetGameObject, float zRange) {
            if(targetGameObject == null) {
                return false;
            }

            float targetDepth = GetGroundDepth(targetGameObject);
            return Mathf.Abs(targetDepth - groundPos) < zRange;
        }

        // SECTION: MOVEMENT, JUMP AND COLLISION

        private Vector3 GetUnitPosition() {
            if(_rigidbody != null) {
                return _rigidbody.position;
            }
            return transform.position;
        }

        private void MoveUnit(Vector3 desiredDelta, bool resolveEnvironmentCollision = true) {
            if(desiredDelta.sqrMagnitude <= Mathf.Epsilon) {
                return;
            }

            Vector3 currentPosition = GetUnitPosition();
            Vector3 resolvedDelta = resolveEnvironmentCollision
                ? ResolveEnvironmentCollisionDelta(currentPosition, desiredDelta)
                : desiredDelta;
            Vector3 nextPosition = currentPosition + resolvedDelta;

            if(_rigidbody != null) {
                _rigidbody.MovePosition(nextPosition);
            }
            transform.position = nextPosition;
            groundPos = nextPosition.z;
            if(isGrounded) {
                baseHeight = nextPosition.y;
            }
        }

        private void SetUnitPosition(Vector3 desiredPosition, bool resolveEnvironmentCollision = true) {
            Vector3 currentPosition = GetUnitPosition();
            MoveUnit(desiredPosition - currentPosition, resolveEnvironmentCollision);
        }

        private Vector3 ResolveEnvironmentCollisionDelta(Vector3 currentPosition, Vector3 desiredDelta) {
            if(_capsuleCollider == null || desiredDelta.sqrMagnitude <= Mathf.Epsilon) {
                return desiredDelta;
            }

            int environmentMask = LayerMask.GetMask("Environment");
            if(environmentMask == 0) {
                return desiredDelta;
            }

            float distance = desiredDelta.magnitude;
            Vector3 direction = desiredDelta / distance;
            GetCapsuleWorldPoints(currentPosition, out Vector3 point1, out Vector3 point2);

            bool blocked = Physics.CapsuleCast(
                point1,
                point2,
                Mathf.Max(0.01f, _capsuleCollider.radius - CAPSULE_CAST_SKIN),
                direction,
                out RaycastHit hit,
                distance + CAPSULE_CAST_SKIN,
                environmentMask,
                QueryTriggerInteraction.Ignore);

            if(!blocked) {
                return desiredDelta;
            }

            if(TryResolveStepUpDelta(currentPosition, desiredDelta, direction, distance, hit, out Vector3 stepUpDelta)) {
                return stepUpDelta;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - CAPSULE_CAST_SKIN);
            return direction * Mathf.Min(distance, allowedDistance);
        }

        private void GetCapsuleWorldPoints(Vector3 position, out Vector3 point1, out Vector3 point2) {
            if(_capsuleCollider == null) {
                point1 = position;
                point2 = position;
                return;
            }

            Vector3 center = position + _capsuleCollider.center;
            float radius = Mathf.Max(0.01f, _capsuleCollider.radius);
            float halfHeight = Mathf.Max(radius, _capsuleCollider.height * 0.5f);
            float sideOffset = Mathf.Max(0f, halfHeight - radius);
            point1 = center + Vector3.up * sideOffset;
            point2 = center - Vector3.up * sideOffset;
        }

        private bool TryResolveStepUpDelta(Vector3 currentPosition, Vector3 desiredDelta, Vector3 direction, float distance, RaycastHit hit, out Vector3 stepUpDelta) {
            stepUpDelta = Vector3.zero;

            if(!isGrounded || _capsuleCollider == null) {
                return false;
            }

            if(Mathf.Abs(desiredDelta.y) > INPUT_DEADZONE) {
                return false;
            }

            float obstacleTop = hit.collider != null ? hit.collider.bounds.max.y : currentPosition.y;
            float requiredStepHeight = obstacleTop - currentPosition.y;
            if(requiredStepHeight <= CAPSULE_CAST_SKIN || requiredStepHeight > DEFAULT_STEP_HEIGHT) {
                return false;
            }

            Vector3 raisedPosition = currentPosition + Vector3.up * (requiredStepHeight + CAPSULE_CAST_SKIN);
            GetCapsuleWorldPoints(raisedPosition, out Vector3 raisedPoint1, out Vector3 raisedPoint2);

            int environmentMask = LayerMask.GetMask("Environment");
            bool blockedAtRaisedPosition = Physics.CapsuleCast(
                raisedPoint1,
                raisedPoint2,
                Mathf.Max(0.01f, _capsuleCollider.radius - CAPSULE_CAST_SKIN),
                direction,
                distance + CAPSULE_CAST_SKIN,
                environmentMask,
                QueryTriggerInteraction.Ignore);

            if(blockedAtRaisedPosition) {
                return false;
            }

            stepUpDelta = desiredDelta + Vector3.up * (requiredStepHeight + CAPSULE_CAST_SKIN);
            return true;
        }

        private bool TryGetGroundContact(out RaycastHit groundHit) {
            groundHit = default;
            if(_capsuleCollider == null) {
                return false;
            }

            Vector3 position = GetUnitPosition();
            GetCapsuleWorldPoints(position, out Vector3 point1, out Vector3 point2);
            float castRadius = Mathf.Max(0.01f, _capsuleCollider.radius - CAPSULE_CAST_SKIN);
            float castDistance = GROUNDED_CHECK_DISTANCE + CAPSULE_CAST_SKIN;
            int groundMask = GetGroundPhysicsMask();

            bool foundGround = Physics.CapsuleCast(
                point1,
                point2,
                castRadius,
                Vector3.down,
                out RaycastHit hitInfo,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            if(!foundGround) {
                return false;
            }

            float groundDot = Vector3.Dot(hitInfo.normal, Vector3.up);
            if(groundDot < GROUND_NORMAL_THRESHOLD) {
                return false;
            }

            groundHit = hitInfo;
            return true;
        }

        private void ApplyAdditionalAirGravity() {
            if(_rigidbody == null || !_rigidbody.useGravity || settings == null) {
                return;
            }

            float desiredGravity = Mathf.Max(0f, settings.jumpGravity * settings.jumpSpeed);
            float worldGravity = Mathf.Abs(Physics.gravity.y);
            float extraGravity = desiredGravity - worldGravity;
            if(extraGravity <= 0f) {
                return;
            }

            _rigidbody.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }

        private void TickGroundingAndGravity() {
            if(settings == null) {
                return;
            }

            if(_rigidbody == null) {
                groundPos = transform.position.z;
                if(isGrounded) {
                    baseHeight = transform.position.y;
                }
                return;
            }

            Vector3 velocity = GetLinearVelocity();
            bool isMovingUp = velocity.y > GROUND_STICK_MAX_UPWARD;
            bool groundedByContact = TryGetGroundContact(out RaycastHit groundHit);
            bool groundedThisFrame = groundedByContact && !isMovingUp;

            if(groundedThisFrame) {
                isGrounded = true;
                float halfHeight = Mathf.Max(_capsuleCollider.radius, _capsuleCollider.height * 0.5f);
                baseHeight = groundHit.point.y + halfHeight - _capsuleCollider.center.y;
                yForce = 0f;

                if(velocity.y < 0f) {
                    velocity.y = 0f;
                    SetLinearVelocity(velocity);
                }
            } else {
                isGrounded = false;
                yForce = velocity.y;
                ApplyAdditionalAirGravity();
            }

            groundPos = GetUnitPosition().z;
        }

        public void MoveToVector(Vector2 moveDir, float moveSpeed) {
            if(settings == null) {
                return;
            }

            if(isGrounded) {
                groundPos = transform.position.z;
                baseHeight = transform.position.y;
            }

            if(_knockbackActive) {
                return;
            }

            if(settings.useAcceleration) {
                if(moveDir.magnitude > INPUT_DEADZONE) {
                    _currentSpeed = Mathf.Min(_currentSpeed + settings.moveAcceleration * Time.fixedDeltaTime, moveSpeed);
                }
            } else if(moveDir.magnitude > INPUT_DEADZONE) {
                _currentSpeed = moveSpeed;
            }

            Vector3 moveDirection3D = new Vector3(moveDir.x, 0f, moveDir.y);
            if(moveDirection3D.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                return;
            }

            moveDirection3D.Normalize();
            _lastGroundMoveDirection = moveDirection3D;

            Vector3 velocity = GetLinearVelocity();
            velocity.x = moveDirection3D.x * _currentSpeed;
            velocity.z = moveDirection3D.z * _currentSpeed;
            if(isGrounded && velocity.y < 0f) {
                velocity.y = 0f;
            }
            SetLinearVelocity(velocity);

            if(Mathf.Abs(moveDir.x) > INPUT_DEADZONE) {
                TurnToDir(moveDir.x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT);
            }
        }

        public bool WallDetected(Vector2 dir) {
            Vector3 direction3D = new Vector3(dir.x, 0f, dir.y).normalized;
            if(direction3D.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                return false;
            }

            Vector3 currentUnitPosition = GetUnitPosition();
            float checkDistance = Mathf.Max(DEFAULT_WALL_CHECK_DISTANCE, dir.magnitude);
            int environmentMask = GetGroundPhysicsMask();

            if(_capsuleCollider == null) {
                Vector3 rayOrigin = currentUnitPosition + Vector3.up * 0.1f;
                bool rayHit = Physics.Raycast(rayOrigin, direction3D, out RaycastHit rayHitInfo, checkDistance, environmentMask, QueryTriggerInteraction.Ignore);
                if(!rayHit) {
                    return false;
                }

                float wallSlope = Vector3.Dot(rayHitInfo.normal, Vector3.up);
                return wallSlope < GROUND_NORMAL_THRESHOLD;
            }

            GetCapsuleWorldPoints(currentUnitPosition, out Vector3 point1, out Vector3 point2);
            float castRadius = Mathf.Max(0.01f, _capsuleCollider.radius - CAPSULE_CAST_SKIN);

            bool blocked = Physics.CapsuleCast(
                point1,
                point2,
                castRadius,
                direction3D,
                out RaycastHit hitInfo,
                checkDistance,
                environmentMask,
                QueryTriggerInteraction.Ignore);

            if(!blocked) {
                return false;
            }

            float obstacleTop = hitInfo.collider != null ? hitInfo.collider.bounds.max.y : currentUnitPosition.y;
            float stepHeight = obstacleTop - currentUnitPosition.y;
            bool obstacleCanBeStepped = isGrounded && stepHeight > CAPSULE_CAST_SKIN && stepHeight <= DEFAULT_STEP_HEIGHT;
            if(obstacleCanBeStepped) {
                return false;
            }

            float slopeDot = Vector3.Dot(hitInfo.normal, Vector3.up);
            return slopeDot < GROUND_NORMAL_THRESHOLD;
        }

        public Vector2 GetWallCheckDistance() {
            float xDistance = DEFAULT_WALL_CHECK_DISTANCE;
            float zDistance = DEFAULT_WALL_CHECK_DISTANCE;

            if(settings != null && settings.hitBox != null) {
                Bounds bounds = settings.hitBox.bounds;
                xDistance = Mathf.Max(xDistance, bounds.extents.x);
                zDistance = Mathf.Max(zDistance, 0.3f);
            }
            return new Vector2(xDistance, zDistance);
        }

        public void AddForce(float force) {
            StartCoroutine(AddForceRoutine(force, 0.25f));
        }

        private IEnumerator AddForceRoutine(float force, float duration) {
            Vector3 startPosition = GetUnitPosition();
            Vector3 endPosition = startPosition + Vector3.right * (int)dir * force;
            float t = 0f;

            while(t < 1f) {
                Vector3 targetPosition = Vector3.Lerp(startPosition, endPosition, MathUtilities.Sinerp(t));
                SetUnitPosition(targetPosition, true);
                t += Time.deltaTime / duration;
                yield return null;
            }
            SetUnitPosition(endPosition, true);
        }

        public void JumpSequence() {
            if(settings == null) {
                return;
            }

            Vector2 inputVector = InputManager.GetInputVector(settings.playerId);

            if(Mathf.Abs(inputVector.x) > INPUT_DEADZONE) {
                TurnToDir(inputVector.x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT);
            }

            Vector3 velocity = GetLinearVelocity();
            velocity.x = inputVector.x * settings.MoveSpeedAirFromStats;
            velocity.z = inputVector.y * settings.MoveSpeedAirFromStats * settings.depthMoveMultiplier;
            SetLinearVelocity(velocity);
            yForce = velocity.y;
        }

        public void StartPhysicalJump() {
            if(settings == null) {
                return;
            }

            SetGravityEnabled(true);
            Vector3 velocity = GetLinearVelocity();
            float desiredGravity = Mathf.Max(0.01f, settings.jumpGravity * settings.jumpSpeed);
            float jumpVelocity = Mathf.Sqrt(2f * desiredGravity * Mathf.Max(0.01f, settings.jumpHeight));
            jumpVelocity = Mathf.Max(MIN_JUMP_VELOCITY, jumpVelocity);
            velocity.y = jumpVelocity;
            SetLinearVelocity(velocity);

            yForce = jumpVelocity;
            isGrounded = false;
            groundPos = transform.position.z;
            baseHeight = transform.position.y;
        }

        public float GetAnimDuration(string animName) {
            if(animator == null || string.IsNullOrEmpty(animName)) {
                return _animDuration;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if(stateInfo.IsName(animName) && stateInfo.length > 0f) {
                _animDuration = stateInfo.length;
            }
            return _animDuration;
        }

        public void StopMoving(bool stopInstantly = true) {
            if(settings == null) {
                return;
            }

            if(isGrounded) {
                groundPos = transform.position.z;
                baseHeight = transform.position.y;
            }

            if(!settings.useAcceleration) {
                stopInstantly = true;
            }

            Vector3 velocity = GetLinearVelocity();

            if(stopInstantly) {
                _currentSpeed = 0f;
                _attackAdvanceActive = false;
                _attackAdvanceVelocity = Vector3.zero;
                velocity.x = 0f;
                velocity.z = 0f;
                if(isGrounded && velocity.y < 0f) {
                    velocity.y = 0f;
                }
                SetLinearVelocity(velocity);
                return;
            }

            _currentSpeed = Mathf.Max(_currentSpeed - settings.moveDeceleration * Time.fixedDeltaTime, 0f);
            velocity.x = _lastGroundMoveDirection.x * _currentSpeed;
            velocity.z = _lastGroundMoveDirection.z * _currentSpeed;
            if(isGrounded && velocity.y < 0f) {
                velocity.y = 0f;
            }
            SetLinearVelocity(velocity);
        }

        public void MoveDashToVector(Vector2 moveDir, float moveSpeed) {
            if(settings == null) {
                return;
            }

            Vector2 dashVector = moveDir;
            if(dashVector.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                dashVector = new Vector2((int)dir, 0f);
            } else {
                dashVector.Normalize();
            }

            Vector3 velocity = GetLinearVelocity();
            velocity.x = dashVector.x * moveSpeed;
            velocity.z = dashVector.y * moveSpeed * settings.depthMoveMultiplier;
            if(isGrounded && velocity.y < 0f) {
                velocity.y = 0f;
            }

            SetLinearVelocity(velocity);
        }

        public void SetVerticalVelocity(float verticalVelocity) {
            Vector3 velocity = GetLinearVelocity();
            velocity.y = verticalVelocity;
            SetLinearVelocity(velocity);
            yForce = verticalVelocity;
        }

        public Vector3 GetCurrentVelocity() {
            return GetLinearVelocity();
        }

        public void SetCurrentVelocity(Vector3 velocity) {
            SetLinearVelocity(velocity);
            yForce = velocity.y;
        }

        public void SetHorizontalVelocity(float xVelocity, float zVelocity) {
            Vector3 velocity = GetLinearVelocity();
            velocity.x = xVelocity;
            velocity.z = zVelocity;
            if(isGrounded && velocity.y < 0f) {
                velocity.y = 0f;
            }
            SetCurrentVelocity(velocity);
        }

        public void SetGravityEnabled(bool enabled) {
            if(_rigidbody == null) {
                return;
            }
            _rigidbody.useGravity = enabled;
        }

        public bool IsDefending(DIRECTION attackDir) {
            DEFENSERESULT defenseResult = ResolveIncomingAttack(attackDir, null, null);
            return defenseResult == DEFENSERESULT.BLOCKED || defenseResult == DEFENSERESULT.PARRIED;
        }

        public bool HitBoxActive() {
            return settings != null && settings.hitBox != null && settings.hitBox.gameObject.activeSelf;
        }

        // SECTION: VISUALS, AUDIO, EFFECTS

        public void ShowHitEffectAtPosition(Vector3 pos) {
            if(settings == null || settings.hitEffect == null) {
                return;
            }

            Instantiate(settings.hitEffect, pos, Quaternion.identity);
        }

        public void PlaySFX(string sfx) {
            AudioController.PlaySFX(sfx, transform.position);
        }

        public void Footstep() {
            Vector3 origin = new Vector3(transform.position.x, baseHeight + 0.1f, groundPos);
            Collider[] overlappedColliders = Physics.OverlapSphere(origin, FOOTSTEP_OVERLAP_RADIUS);

            for(int i = 0; i < overlappedColliders.Length; i++) {
                Surface surface = overlappedColliders[i].GetComponent<Surface>();
                if(surface != null && !string.IsNullOrEmpty(surface.footstepSFX)) {
                    AudioController.PlaySFX(surface.footstepSFX, transform.position);
                    return;
                }
            }

            AudioController.PlaySFX("FootstepDefault", transform.position);
        }

        public void ShowEffect(string effectName) {
            if(string.IsNullOrEmpty(effectName)) {
                return;
            }

            GameObject effect = Instantiate(Resources.Load(effectName), transform.position, Quaternion.identity) as GameObject;
            if(effect == null) {
                return;
            }

            Destroy(effect, 3f);
        }

        public void SpawnProjectile(string objName) {
            if(string.IsNullOrEmpty(objName)) {
                return;
            }

            WeaponAttachment weaponAttachment = GetComponentInChildren<WeaponAttachment>();
            Vector3 spawnPos = weaponAttachment != null ? weaponAttachment.transform.position : transform.position;

            GameObject projectile = Instantiate(Resources.Load(objName), spawnPos, Quaternion.identity) as GameObject;
            if(projectile == null) {
                return;
            }

            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if(projectileComponent == null) {
                return;
            }
            projectileComponent.dir = dir;
            if(projectileComponent.attackData != null) {
                projectileComponent.attackData.inflictor = gameObject;
            }
        }

        public void CamShake() {
            Camera.main?.GetComponent<CameraShake>()?.ShowCamShake();
        }

        // SECTION: FOV, DASH AND HELPERS

        public bool targetInSight() {
            if(target == null || settings == null) {
                return false;
            }

            if(!IsValidTargetForCombat(target)) {
                target = null;
                targetSpotted = false;
                return false;
            }

            if(!settings.enableFOV) {
                targetSpotted = true;
                return true;
            }

            Vector3 origin = GetFovOrigin();
            Vector3 targetGroundPosition = new Vector3(target.transform.position.x, origin.y, GetGroundDepth(target));
            Vector3 directionToTarget = targetGroundPosition - origin;
            directionToTarget.y = 0f;

            float distanceToTarget = directionToTarget.magnitude;
            if(distanceToTarget > settings.viewDistance) {
                return false;
            }

            Vector3 facingDirection = dir == DIRECTION.RIGHT ? Vector3.right : Vector3.left;
            float angleToTarget = Vector3.Angle(facingDirection, directionToTarget);
            bool inSight = angleToTarget <= settings.viewAngle * 0.5f;
            if(inSight) {
                targetSpotted = true;
            }
            return inSight;
        }

        private void OnDrawGizmos() {
            if(settings == null || !settings.showFOVCone || settings.viewDistance <= 0f) {
                return;
            }

            int lineSegments = settings.viewAngle > 180f ? 40 : 20;
            Gizmos.color = Color.red;
            Vector3 origin = GetFovOrigin();
            Vector3 forward = dir == DIRECTION.RIGHT ? Vector3.right : Vector3.left;

            float halfAngle = settings.viewAngle * 0.5f;
            Vector3 previousPoint = origin + Quaternion.Euler(0f, -halfAngle, 0f) * forward * settings.viewDistance;

            for(int i = 0; i <= lineSegments; i++) {
                float angle = -halfAngle + (settings.viewAngle / lineSegments) * i;
                Vector3 nextPoint = origin + Quaternion.Euler(0f, angle, 0f) * forward * settings.viewDistance;
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }

            Vector3 leftBoundary = origin + Quaternion.Euler(0f, halfAngle, 0f) * forward * settings.viewDistance;
            Vector3 rightBoundary = origin + Quaternion.Euler(0f, -halfAngle, 0f) * forward * settings.viewDistance;
            Gizmos.DrawLine(origin, leftBoundary);
            Gizmos.DrawLine(origin, rightBoundary);
        }

        public bool DashInputDetected(int playerId) {
            if(settings == null || !settings.canDash) {
                return false;
            }

            float horizontal = InputManager.GetInputVector(playerId).x;
            bool isPressed = Mathf.Abs(horizontal) >= DASH_INPUT_THRESHOLD;
            bool pressedThisFrame = isPressed && !_dashInputWasPressed;
            _dashInputWasPressed = isPressed;

            if(!pressedThisFrame) {
                return false;
            }

            float currentTapSign = Mathf.Sign(horizontal);
            bool sameDirectionTap = Mathf.Abs(currentTapSign - _lastHorizontalTapSign) <= Mathf.Epsilon;
            bool withinTapWindow = Time.time - _lastHorizontalTapTime <= DOUBLE_TAP_WINDOW;

            _lastHorizontalTapSign = currentTapSign;
            _lastHorizontalTapTime = Time.time;

            if(!sameDirectionTap || !withinTapWindow) {
                return false;
            }
            return IsDashAvailable;
        }

        public void MarkDashUsed() {
            _lastDashTime = Time.time;
        }

        public DIRECTION GetDashDirectionFromInput(int playerId) {
            float x = InputManager.GetInputVector(playerId).x;
            if(Mathf.Abs(x) <= INPUT_DEADZONE) {
                return dir;
            }
            return x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT;
        }

        public void StartGhostTrail(float duration, float interval) {
            StopGhostTrail();
            _ghostTrailRoutine = StartCoroutine(GhostTrailRoutine(duration, interval));
        }

        public void StopGhostTrail() {
            if(_ghostTrailRoutine == null) {
                return;
            }
            StopCoroutine(_ghostTrailRoutine);
            _ghostTrailRoutine = null;
        }

        private IEnumerator GhostTrailRoutine(float duration, float interval) {
            if(_spriteRenderer == null || interval <= 0f) {
                yield break;
            }

            float endTime = Time.time + duration;
            while(Time.time <= endTime) {
                SpawnGhostFrame();
                yield return new WaitForSeconds(interval);
            }
            _ghostTrailRoutine = null;
        }

        private void SpawnGhostFrame() {
            if(_spriteRenderer == null || _spriteRenderer.sprite == null) {
                return;
            }

            EnsureGhostPoolInitialized();
            if(_ghostFramePool == null) {
                return;
            }

            GhostFrame ghostFrame = _ghostFramePool.Get();
            if(ghostFrame == null || ghostFrame.gameObject == null || ghostFrame.renderer == null) {
                return;
            }

            ghostFrame.gameObject.transform.position = _spriteRenderer.transform.position;
            ghostFrame.gameObject.transform.rotation = _spriteRenderer.transform.rotation;
            ghostFrame.gameObject.transform.localScale = _spriteRenderer.transform.lossyScale;

            ghostFrame.renderer.sprite = _spriteRenderer.sprite;
            ghostFrame.renderer.flipX = _spriteRenderer.flipX;
            ghostFrame.renderer.flipY = _spriteRenderer.flipY;
            ghostFrame.renderer.color = new Color(1f, 1f, 1f, 0.45f);

            Camera mainCamera = Camera.main;
            if(mainCamera != null) {
                ghostFrame.gameObject.transform.position -= mainCamera.transform.forward * 0.01f;
            }

            ghostFrame.releaseTime = Time.time + DEFAULT_GHOST_LIFETIME;
            _activeGhostFrames.Add(ghostFrame);
        }

        private void EnsureGhostPoolInitialized() {
            if(_ghostFramePool != null) {
                return;
            }

            _ghostFramePool = new ObjectPool<GhostFrame>(
                CreateGhostFrame,
                OnGetGhostFrame,
                OnReleaseGhostFrame,
                OnDestroyGhostFrame,
                false,
                GHOST_POOL_DEFAULT_CAPACITY,
                GHOST_POOL_MAX_SIZE);
        }

        private GhostFrame CreateGhostFrame() {
            GameObject ghostObject = new GameObject($"{name}_DashGhost");
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            ghostObject.SetActive(false);

            GhostFrame frame = new GhostFrame();
            frame.gameObject = ghostObject;
            frame.renderer = ghostRenderer;
            frame.releaseTime = 0f;
            return frame;
        }

        private void OnGetGhostFrame(GhostFrame frame) {
            if(frame == null || frame.gameObject == null) {
                return;
            }
            frame.gameObject.SetActive(true);
        }

        private void OnReleaseGhostFrame(GhostFrame frame) {
            if(frame == null || frame.gameObject == null) {
                return;
            }
            frame.gameObject.SetActive(false);
        }

        private void OnDestroyGhostFrame(GhostFrame frame) {
            if(frame == null || frame.gameObject == null) {
                return;
            }
            Destroy(frame.gameObject);
        }

        private void TickGhostFrames() {
            if(_activeGhostFrames.Count == 0 || _ghostFramePool == null) {
                return;
            }

            float now = Time.time;
            for(int i = _activeGhostFrames.Count - 1; i >= 0; i--) {
                GhostFrame frame = _activeGhostFrames[i];
                if(frame == null || frame.gameObject == null || now < frame.releaseTime) {
                    continue;
                }

                _activeGhostFrames.RemoveAt(i);
                _ghostFramePool.Release(frame);
            }
        }

        private void ReleaseAllActiveGhostFrames() {
            if(_ghostFramePool == null) {
                _activeGhostFrames.Clear();
                return;
            }

            for(int i = _activeGhostFrames.Count - 1; i >= 0; i--) {
                GhostFrame frame = _activeGhostFrames[i];
                if(frame != null && frame.gameObject != null) {
                    _ghostFramePool.Release(frame);
                }
            }
            _activeGhostFrames.Clear();
        }

        private void CleanupGhostPool() {
            StopGhostTrail();
            ReleaseAllActiveGhostFrames();

            if(_ghostFramePool == null) {
                return;
            }

            _ghostFramePool.Clear();
            _ghostFramePool = null;
        }

        private Vector3 GetLinearVelocity() {
            if(_rigidbody == null) {
                return Vector3.zero;
            }

            #if UNITY_6000_0_OR_NEWER
                return _rigidbody.linearVelocity;
            #else
                return _rigidbody.velocity;
            #endif
        }

        private void SetLinearVelocity(Vector3 velocity) {
            if(_rigidbody == null) {
                return;
            }

            #if UNITY_6000_0_OR_NEWER
                _rigidbody.linearVelocity = velocity;
            #else
                _rigidbody.velocity = velocity;
            #endif
        }

        private void OnApplicationQuit() {
            _onApplicationQuit = true;
        }

        private float GetGroundDepth(GameObject obj) {
            UnitActions unitActions = obj.GetComponent<UnitActions>();
            if(unitActions != null) {
                return unitActions.groundPos;
            }
            return obj.transform.position.z;
        }

        private GameObject GetTouchingEnemyCandidate() {
            if(_capsuleCollider == null) {
                return null;
            }

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject nearestEnemy = null;
            float nearestDistanceSqr = float.MaxValue;

            for(int i = 0; i < enemies.Length; i++) {
                GameObject enemy = enemies[i];
                if(!CanAutoGrabEnemy(enemy)) {
                    continue;
                }

                UnitActions enemyActions = enemy.GetComponent<UnitActions>();
                float enemyDepth = enemyActions != null ? enemyActions.groundPos : enemy.transform.position.z;
                if(Mathf.Abs(enemyDepth - groundPos) > AUTO_GRAB_DEPTH_RANGE) {
                    continue;
                }

                Collider enemyCollider = enemy.GetComponent<Collider>();
                if(enemyCollider == null || !_capsuleCollider.bounds.Intersects(enemyCollider.bounds)) {
                    continue;
                }

                Vector2 distanceVector = new Vector2(
                    enemy.transform.position.x - transform.position.x,
                    enemyDepth - groundPos);
                float distanceSqr = distanceVector.sqrMagnitude;
                if(distanceSqr < nearestDistanceSqr) {
                    nearestDistanceSqr = distanceSqr;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }

        private bool CanAutoGrabEnemy(GameObject enemy) {
            if(enemy == null || enemy == gameObject || !enemy.activeInHierarchy) {
                return false;
            }

            HealthSystem healthSystem = enemy.GetComponent<HealthSystem>();
            if(healthSystem != null && healthSystem.isDead) {
                return false;
            }

            UnitSettings enemySettings = enemy.GetComponent<UnitSettings>();
            if(enemySettings == null || !enemySettings.canBeGrabbed) {
                return false;
            }

            StateMachine enemyStateMachine = enemy.GetComponent<StateMachine>();
            State enemyState = enemyStateMachine != null ? enemyStateMachine.GetCurrentState() : null;
            if(enemyState == null || !enemyState.canGrab) {
                return false;
            }

            UnitActions enemyActions = enemy.GetComponent<UnitActions>();
            if(enemyActions == null || !enemyActions.isGrounded) {
                return false;
            }

            return true;
        }

        private Vector3 GetFovOrigin() {
            if(settings == null) {
                return transform.position;
            }

            float xOffset = settings.viewPosOffset.x * (int)dir;
            float zOffset = settings.viewPosOffset.y;
            return new Vector3(
                transform.position.x + xOffset,
                transform.position.y + settings.viewHeightOffset,
                groundPos + zOffset
            );
        }

        private static void AppendObjectsWithTag(List<GameObject> targetList, string tag) {
            GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
            for(int i = 0; i < found.Length; i++) {
                if(found[i] != null) {
                    targetList.Add(found[i]);
                }
            }
        }

        private void SortByDistance(List<GameObject> objectsToSort) {
            objectsToSort.Sort((a, b) => {
                if(a == null || b == null) {
                    return 0;
                }

                float distanceA = Vector2.Distance(currentPosition, new Vector2(a.transform.position.x, GetGroundDepth(a)));
                float distanceB = Vector2.Distance(currentPosition, new Vector2(b.transform.position.x, GetGroundDepth(b)));
                return distanceA.CompareTo(distanceB);
            });
        }
    }

    public enum DIRECTION {
        LEFT = -1,
        RIGHT = 1,
    }
}

