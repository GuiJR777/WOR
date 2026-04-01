// Purpose: Moves an enemy to a destination using NavMesh pathfinding.
using UnityEngine;

namespace WOR.Gameplay {

    public class EnemyMoveTo : State {

        private const string RUN_ANIMATION = "Run";
        private const string IDLE_ANIMATION = "Idle";
        private const float ARRIVAL_DISTANCE = 0.1f;

        private readonly Vector2 _destination;
        private EnemyPathfindingNavigator _pathNavigator;

        public EnemyMoveTo(Vector2 destination) {
            _destination = destination;
        }

        public override void Enter() {
            _pathNavigator = new EnemyPathfindingNavigator(unit);
            _pathNavigator.SetTarget(GetWorldDestination(), ARRIVAL_DISTANCE);
        }

        public override void FixedUpdate() {
            if(_pathNavigator == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            if(_pathNavigator.ReachedTarget()) {
                unit.stateMachine.SetState(new EnemyIdle());
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

        private Vector3 GetWorldDestination() {
            return new Vector3(_destination.x, unit.transform.position.y, _destination.y);
        }
    }
}
