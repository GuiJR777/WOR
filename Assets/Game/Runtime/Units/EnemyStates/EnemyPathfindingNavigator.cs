// Purpose: Provides reusable 2.5D NavMesh pathfinding movement for enemy state logic.
using UnityEngine;
using UnityEngine.AI;

namespace WOR.Gameplay {

    public sealed class EnemyPathfindingNavigator {

        private const float MIN_STOPPING_DISTANCE = 0.01f;
        private const float CORNER_REACH_DISTANCE = 0.12f;
        private const float REPATH_INTERVAL = 0.2f;
        private const float REPATH_DISTANCE_THRESHOLD = 0.2f;
        private const float NAVMESH_SAMPLE_DISTANCE = 1f;
        private const float STUCK_MIN_PROGRESS_DISTANCE = 0.01f;
        private const float STUCK_REPATH_DELAY = 0.2f;
        private const float STUCK_BLOCKED_DELAY = 0.8f;
        private const float INVALID_TIME = -999f;

        private readonly UnitActions _unit;
        private readonly NavMeshPath _navPath = new NavMeshPath();

        private bool _hasTarget;
        private bool _pathBlocked;
        private bool _pathDirty;
        private float _stoppingDistance = MIN_STOPPING_DISTANCE;
        private float _lastPathBuildTime = INVALID_TIME;
        private int _currentCornerIndex;

        private Vector3 _targetPosition;
        private Vector3 _lastPathTargetPosition;
        private Vector2 _lastMovementSamplePosition;
        private bool _hasMovementSample;
        private float _stuckTimer;

        public bool HasTarget => _hasTarget;
        public bool IsPathBlocked => _pathBlocked;

        public EnemyPathfindingNavigator(UnitActions unit) {
            _unit = unit;
        }

        public void SetTarget(Vector3 worldTarget, float stoppingDistance) {
            _targetPosition = worldTarget;
            _stoppingDistance = Mathf.Max(MIN_STOPPING_DISTANCE, stoppingDistance);
            _lastPathTargetPosition = worldTarget;
            _hasTarget = true;
            _pathBlocked = false;
            _pathDirty = true;
            _lastPathBuildTime = INVALID_TIME;
            _currentCornerIndex = 0;
            ResetStuckDetection();

            RebuildPathNow();
        }

        public void UpdateTarget(Vector3 worldTarget) {
            if(!_hasTarget) {
                return;
            }

            float sqrDistanceToPrevious = (worldTarget - _lastPathTargetPosition).sqrMagnitude;
            if(sqrDistanceToPrevious < REPATH_DISTANCE_THRESHOLD * REPATH_DISTANCE_THRESHOLD) {
                return;
            }

            _targetPosition = worldTarget;
            _pathDirty = true;
        }

        public void ClearTarget() {
            _hasTarget = false;
            _pathBlocked = false;
            _pathDirty = false;
            _currentCornerIndex = 0;
            ResetStuckDetection();
        }

        public bool ReachedTarget() {
            if(!_hasTarget || _unit == null) {
                return false;
            }

            Vector2 unitPosition = new Vector2(_unit.transform.position.x, _unit.groundPos);
            Vector2 targetPosition = new Vector2(_targetPosition.x, _targetPosition.z);
            float distance = Vector2.Distance(unitPosition, targetPosition);
            return distance <= _stoppingDistance;
        }

        public bool TryGetMoveDirection(out Vector2 moveDirection) {
            moveDirection = Vector2.zero;

            if(!_hasTarget || _unit == null) {
                return false;
            }

            RebuildPathWhenNeeded();

            if(_pathBlocked || _navPath.corners == null || _navPath.corners.Length == 0) {
                return false;
            }

            if(IsStuckWithoutProgress()) {
                return false;
            }

            while(_currentCornerIndex < _navPath.corners.Length) {
                Vector3 currentCorner = _navPath.corners[_currentCornerIndex];
                Vector2 cornerDirection = new Vector2(
                    currentCorner.x - _unit.transform.position.x,
                    currentCorner.z - _unit.groundPos);

                if(cornerDirection.sqrMagnitude <= CORNER_REACH_DISTANCE * CORNER_REACH_DISTANCE) {
                    _currentCornerIndex++;
                    continue;
                }

                Vector2 normalizedDirection = cornerDirection.normalized;
                if(IsImmediateWallDetected(normalizedDirection)) {
                    _pathDirty = true;
                    return false;
                }

                moveDirection = normalizedDirection;
                return true;
            }

            if(!ReachedTarget()) {
                _pathDirty = true;
            }

            return false;
        }

        private void RebuildPathWhenNeeded() {
            if(!_hasTarget) {
                return;
            }

            bool intervalElapsed = Time.time - _lastPathBuildTime >= REPATH_INTERVAL;
            if(!_pathDirty && !intervalElapsed) {
                return;
            }

            RebuildPathNow();
        }

        private void RebuildPathNow() {
            if(_unit == null || !_hasTarget) {
                _pathBlocked = true;
                return;
            }

            Vector3 unitWorldPosition = _unit.transform.position;
            Vector3 pathStart = new Vector3(unitWorldPosition.x, unitWorldPosition.y, _unit.groundPos);

            if(!TrySampleNavMesh(pathStart, out Vector3 sampledStart) ||
               !TrySampleNavMesh(_targetPosition, out Vector3 sampledTarget)) {
                MarkPathAsBlocked();
                return;
            }

            bool pathComputed = NavMesh.CalculatePath(sampledStart, sampledTarget, NavMesh.AllAreas, _navPath);
            _lastPathBuildTime = Time.time;
            _lastPathTargetPosition = _targetPosition;
            _pathDirty = false;
            _currentCornerIndex = 0;

            if(!pathComputed || _navPath.corners == null || _navPath.corners.Length == 0 ||
               _navPath.status == NavMeshPathStatus.PathInvalid) {
                _pathBlocked = true;
                return;
            }

            _currentCornerIndex = _navPath.corners.Length > 1 ? 1 : 0;
            _pathBlocked = false;
        }

        private void MarkPathAsBlocked() {
            _lastPathBuildTime = Time.time;
            _lastPathTargetPosition = _targetPosition;
            _pathDirty = false;
            _pathBlocked = true;
            _currentCornerIndex = 0;
        }

        private static bool TrySampleNavMesh(Vector3 worldPosition, out Vector3 sampledPosition) {
            bool sampled = NavMesh.SamplePosition(
                worldPosition,
                out NavMeshHit navMeshHit,
                NAVMESH_SAMPLE_DISTANCE,
                NavMesh.AllAreas);

            sampledPosition = sampled ? navMeshHit.position : worldPosition;
            return sampled;
        }

        private bool IsImmediateWallDetected(Vector2 normalizedDirection) {
            if(_unit == null) {
                return false;
            }

            Vector2 wallCheckDistance = _unit.GetWallCheckDistance();
            return _unit.WallDetected(normalizedDirection * wallCheckDistance);
        }

        private bool IsStuckWithoutProgress() {
            if(_unit == null) {
                return false;
            }

            Vector2 currentPosition = new Vector2(_unit.transform.position.x, _unit.groundPos);
            if(!_hasMovementSample) {
                _lastMovementSamplePosition = currentPosition;
                _hasMovementSample = true;
                _stuckTimer = 0f;
                return false;
            }

            float sqrDistance = (currentPosition - _lastMovementSamplePosition).sqrMagnitude;
            if(sqrDistance >= STUCK_MIN_PROGRESS_DISTANCE * STUCK_MIN_PROGRESS_DISTANCE) {
                _lastMovementSamplePosition = currentPosition;
                _stuckTimer = 0f;
                return false;
            }

            _stuckTimer += Time.fixedDeltaTime;
            if(_stuckTimer >= STUCK_REPATH_DELAY) {
                _pathDirty = true;
            }

            if(_stuckTimer >= STUCK_BLOCKED_DELAY) {
                _pathBlocked = true;
            }

            return _stuckTimer >= STUCK_REPATH_DELAY;
        }

        private void ResetStuckDetection() {
            _hasMovementSample = false;
            _stuckTimer = 0f;
            _lastMovementSamplePosition = Vector2.zero;
        }
    }
}
