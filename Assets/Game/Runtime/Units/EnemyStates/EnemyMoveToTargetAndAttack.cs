// Purpose: Moves an enemy into attack range using NavMesh pathfinding, then executes an attack.
using UnityEngine;

namespace WOR.Gameplay {

    public class EnemyMoveToTargetAndAttack : State {

        private const string RUN_ANIMATION = "Run";
        private const string IDLE_ANIMATION = "Idle";
        private const float ATTACK_DISTANCE = 1f;
        private const float ATTACK_ARRIVAL_DISTANCE = 0.1f;
        private static readonly Vector2 MAX_ATTACK_RANGE = new Vector2(1.2f, 0.1f);

        private readonly AttackData _attack;
        private EnemyPathfindingNavigator _pathNavigator;
        private float _pauseBeforeAttack;

        public EnemyMoveToTargetAndAttack(AttackData attack) {
            _attack = attack;
        }

        public override void Enter() {
            if(unit.target == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            _pauseBeforeAttack = unit.settings.enemyPauseBeforeAttack;
            unit.TurnToTarget();

            _pathNavigator = new EnemyPathfindingNavigator(unit);
            _pathNavigator.SetTarget(GetWorldPosition(GetIdealAttackPos()), ATTACK_ARRIVAL_DISTANCE);
        }

        public override void Update() {
            if(unit.target == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            if(!TargetInRange()) {
                return;
            }

            unit.StopMoving();
            unit.animator.Play(IDLE_ANIMATION);

            if(_pauseBeforeAttack > 0f) {
                _pauseBeforeAttack -= Time.deltaTime;
                return;
            }

            unit.stateMachine.SetState(_attack != null ? new EnemyAttack(_attack) : new EnemyIdle());
        }

        public override void FixedUpdate() {
            if(unit.target == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            if(_pathNavigator == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            bool targetIsGrounded = unit.target.GetComponent<UnitActions>().isGrounded;
            bool shouldMoveToAttackPosition = (unit.distanceToTarget().y > MAX_ATTACK_RANGE.y && targetIsGrounded) ||
                                              unit.distanceToTarget().x > MAX_ATTACK_RANGE.x;

            if(!shouldMoveToAttackPosition) {
                unit.StopMoving(false);
                unit.animator.Play(IDLE_ANIMATION);
                return;
            }

            Vector3 idealAttackWorldPosition = GetWorldPosition(GetIdealAttackPos());
            _pathNavigator.UpdateTarget(idealAttackWorldPosition);

            if(_pathNavigator.ReachedTarget()) {
                return;
            }

            if(!_pathNavigator.TryGetMoveDirection(out Vector2 moveDirection)) {
                if(_pathNavigator.IsPathBlocked) {
                    unit.stateMachine.SetState(new EnemyIdle());
                    return;
                }

                unit.StopMoving(false);
                unit.animator.Play(IDLE_ANIMATION);
                return;
            }

            unit.MoveToVector(moveDirection, unit.settings.MoveSpeedFromStats);
            unit.animator.Play(RUN_ANIMATION);
        }

        private Vector2 GetIdealAttackPos() {
            Vector2 directionToTarget = unit.target.transform.position.x > unit.transform.position.x ? Vector2.right : Vector2.left;
            Vector2 targetPosition = unit.target.GetComponent<UnitActions>().currentPosition;
            return targetPosition - directionToTarget * ATTACK_DISTANCE;
        }

        private bool TargetInRange() {
            Vector2 distanceToTarget = unit.distanceToTarget();
            return distanceToTarget.x < MAX_ATTACK_RANGE.x && distanceToTarget.y < MAX_ATTACK_RANGE.y;
        }

        private Vector3 GetWorldPosition(Vector2 position2D) {
            return new Vector3(position2D.x, unit.transform.position.y, position2D.y);
        }
    }
}
