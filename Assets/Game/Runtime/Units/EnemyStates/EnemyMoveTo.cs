using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Moves enemy to a target point on the ground plane.
    //enemy moves towards the target
    public class EnemyMoveTo : State {

        private string animationName = "Run";
        private Vector2 destination;

        public EnemyMoveTo(Vector2 pos){
            destination = pos;
        }

        public override void FixedUpdate(){
            Vector2 unitPos = unit.GetComponent<UnitActions>().currentPosition;
            Vector2 moveDir = (destination - unitPos).normalized; //get vector to destination

            //if there is a wall in front of us, go to Idle
            Vector2 wallDistanceCheck = unit.GetWallCheckDistance();
            if(unit.WallDetected(moveDir * wallDistanceCheck)){
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            //move and play 'Run' anim
            unit.MoveToVector(moveDir, unit.settings.MoveSpeedFromStats);
            unit.animator.Play(animationName);
            
            //if we've reached our destination, go to Idle
            if(Vector2.Distance(unitPos, destination) < .1f) unit.stateMachine.SetState(new EnemyIdle());
        }
    }
}

