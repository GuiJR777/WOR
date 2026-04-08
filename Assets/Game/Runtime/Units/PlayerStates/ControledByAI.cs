// Purpose: Disables player input and drives scripted movement with NavMesh pathfinding toward a target.
using UnityEngine;
using UnityEngine.AI;

namespace WOR.Gameplay {

    public class ControledByAI : State {

        private const string IdleAnimation = "Idle";
        private const string RunAnimation = "Run";
        private const float DefaultStoppingDistance = 0.05f;
        private const float MinStoppingDistance = 0.01f;
        private const float MoveDirectionThreshold = 0.001f;
        private const float RepathInterval = 0.2f;
        private const float RepathDistanceThreshold = 0.2f;
        private const float CornerReachDistance = 0.12f;

        private bool _hasTarget;
        private float _stoppingDistance = DefaultStoppingDistance;
        private bool _pathBlocked;
        private Vector2 _cachedMoveDirection = Vector2.zero;
        private bool _forceAnimationActive;
        private bool _forceAnimationLocksMovement;
        private float _forceAnimationEndTime;

        private Transform _targetTransform;
        private Vector3 _targetPosition;
        private Vector3 _lastPathTargetPosition;

        private NavMeshPath _navPath;
        private bool _pathDirty;
        private float _lastPathBuildTime = -999f;
        private int _currentCornerIndex;

        public override bool canGrab => false;

        public ControledByAI() {
        }

        public ControledByAI(float targetX, float stoppingDistance = DefaultStoppingDistance) {
            WalkToX(targetX, stoppingDistance);
        }

        public ControledByAI(Vector3 targetPosition, float stoppingDistance = DefaultStoppingDistance) {
            WalkToPosition(targetPosition, stoppingDistance);
        }

        public override void Enter() {
            unit.StopMoving(true);
            _forceAnimationActive = false;
            _forceAnimationLocksMovement = false;
            _forceAnimationEndTime = 0f;
            PlayIdle();
        }

        public override void Update() {
            if(IsForcedAnimationActive()) {
                if(_forceAnimationLocksMovement) {
                    unit.StopMoving(true);
                }
                return;
            }

            if(!_hasTarget) {
                _pathBlocked = false;
                _cachedMoveDirection = Vector2.zero;
                unit.StopMoving(false);
                PlayIdle();
                return;
            }

            SyncTargetPositionFromTransform();
            RebuildPathWhenNeeded();

            if(ReachedTarget()) {
                ClearTargetX();
                unit.StopMoving(true);
                TransitionToDefaultState();
                return;
            }

            if(!TryBuildMoveDirectionAlongPath(out Vector2 moveDirection)) {
                _pathBlocked = true;
                _cachedMoveDirection = Vector2.zero;
                unit.StopMoving(true);
                PlayIdle();
                return;
            }

            _pathBlocked = false;
            _cachedMoveDirection = moveDirection;
            unit.TurnToFloatDir(moveDirection.x);
        }

        public override void FixedUpdate() {
            if(IsForcedAnimationActive()) {
                if(_forceAnimationLocksMovement) {
                    unit.StopMoving(true);
                }
                return;
            }

            if(!_hasTarget || _pathBlocked) {
                unit.StopMoving(false);
                return;
            }

            if(_cachedMoveDirection.sqrMagnitude <= MoveDirectionThreshold) {
                unit.StopMoving(false);
                return;
            }

            unit.MoveToVector(_cachedMoveDirection, unit.settings.MoveSpeedFromStats);
            unit.animator.Play(RunAnimation);
        }

        public void PlayForcedAnimation(string animationName, float duration, bool lockMovement = true) {
            if(string.IsNullOrWhiteSpace(animationName) || unit == null || unit.animator == null) {
                return;
            }

            _forceAnimationActive = true;
            _forceAnimationLocksMovement = lockMovement;
            _forceAnimationEndTime = Time.time + Mathf.Max(0.01f, duration);
            unit.animator.Play(animationName, 0, 0f);
        }

        public void WalkToX(float targetX, float stoppingDistance = DefaultStoppingDistance) {
            Vector3 currentPosition = unit != null ? unit.transform.position : Vector3.zero;
            Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);
            WalkToPosition(targetPosition, stoppingDistance);
        }

        public void WalkToPosition(Vector3 targetPosition, float stoppingDistance = DefaultStoppingDistance) {
            _targetTransform = null;
            _targetPosition = targetPosition;
            InitializeTarget(stoppingDistance);
        }

        public void WalkToTransform(Transform targetTransform, float stoppingDistance = DefaultStoppingDistance) {
            if(targetTransform == null) {
                ClearTargetX();
                return;
            }

            _targetTransform = targetTransform;
            _targetPosition = targetTransform.position;
            InitializeTarget(stoppingDistance);
        }

        public void ClearTargetX() {
            _hasTarget = false;
            _pathBlocked = false;
            _cachedMoveDirection = Vector2.zero;
            _pathDirty = false;
            _currentCornerIndex = 0;
        }

        public bool HasTargetX() {
            return _hasTarget;
        }

        public bool ReachedTargetX() {
            return ReachedTarget();
        }

        public bool IsPathBlocked() {
            return _pathBlocked;
        }

        private void InitializeTarget(float stoppingDistance) {
            _stoppingDistance = Mathf.Max(MinStoppingDistance, stoppingDistance);
            _hasTarget = true;
            _pathBlocked = false;
            _pathDirty = true;
            _lastPathBuildTime = -999f;
            _lastPathTargetPosition = _targetPosition;
            EnsureNavPath();
            RebuildPathNow();
        }

        private bool ReachedTarget() {
            if(!_hasTarget || unit == null) {
                return false;
            }

            Vector2 unitPos = new Vector2(unit.transform.position.x, unit.groundPos);
            Vector2 targetPos = new Vector2(_targetPosition.x, _targetPosition.z);
            float distanceToTarget = Vector2.Distance(unitPos, targetPos);
            return distanceToTarget <= _stoppingDistance;
        }

        private void SyncTargetPositionFromTransform() {
            if(_targetTransform == null) {
                return;
            }

            Vector3 targetTransformPosition = _targetTransform.position;
            float sqrDistanceToPrevious = (targetTransformPosition - _lastPathTargetPosition).sqrMagnitude;
            if(sqrDistanceToPrevious >= RepathDistanceThreshold * RepathDistanceThreshold) {
                _targetPosition = targetTransformPosition;
                _pathDirty = true;
            }
        }

        private void RebuildPathWhenNeeded() {
            if(!_hasTarget) {
                return;
            }

            bool intervalElapsed = Time.time - _lastPathBuildTime >= RepathInterval;
            if(!_pathDirty && !intervalElapsed) {
                return;
            }

            RebuildPathNow();
        }

        private void RebuildPathNow() {
            if(unit == null) {
                _pathBlocked = true;
                return;
            }

            EnsureNavPath();

            Vector3 unitPosition = unit.transform.position;
            Vector3 startPosition = new Vector3(unitPosition.x, unitPosition.y, unit.groundPos);
            bool pathComputed = NavMesh.CalculatePath(startPosition, _targetPosition, NavMesh.AllAreas, _navPath);

            _lastPathBuildTime = Time.time;
            _pathDirty = false;
            _lastPathTargetPosition = _targetPosition;
            _currentCornerIndex = 0;

            if(!pathComputed || _navPath == null || _navPath.corners == null || _navPath.corners.Length == 0) {
                _pathBlocked = true;
                return;
            }

            _currentCornerIndex = _navPath.corners.Length > 1 ? 1 : 0;
            _pathBlocked = false;
        }

        private bool TryBuildMoveDirectionAlongPath(out Vector2 moveDirection) {
            moveDirection = Vector2.zero;

            if(_pathBlocked || _navPath == null || _navPath.corners == null || _navPath.corners.Length == 0) {
                return false;
            }

            while(_currentCornerIndex < _navPath.corners.Length) {
                Vector3 currentCorner = _navPath.corners[_currentCornerIndex];
                Vector2 cornerDirection = new Vector2(
                    currentCorner.x - unit.transform.position.x,
                    currentCorner.z - unit.groundPos);

                if(cornerDirection.magnitude <= CornerReachDistance) {
                    _currentCornerIndex++;
                    continue;
                }

                Vector2 normalizedDirection = cornerDirection.normalized;
                Vector2 wallCheckDistance = unit.GetWallCheckDistance();
                if(unit.WallDetected(normalizedDirection * wallCheckDistance)) {
                    _pathDirty = true;
                    return false;
                }

                moveDirection = normalizedDirection;
                return true;
            }

            if(ReachedTarget()) {
                return true;
            }

            _pathDirty = true;
            return false;
        }

        private void EnsureNavPath() {
            if(_navPath == null) {
                _navPath = new NavMeshPath();
            }
        }

        private bool IsForcedAnimationActive() {
            if(!_forceAnimationActive) {
                return false;
            }

            if(Time.time <= _forceAnimationEndTime) {
                return true;
            }

            _forceAnimationActive = false;
            _forceAnimationLocksMovement = false;
            return false;
        }

        private void PlayIdle() {
            unit.animator.Play(IdleAnimation);
        }

        private void TransitionToDefaultState() {
            if(unit == null || unit.stateMachine == null) {
                return;
            }

            if(unit.isPlayer) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            if(unit.isEnemy) {
                unit.stateMachine.SetState(new EnemyIdle());
            }
        }
    }
}
