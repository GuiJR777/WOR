namespace BeatEmUpTemplate2D {

    public class EnemyIdle : State {

        private string animationName = "Idle";
        public override void Enter(){

            unit.StopMoving();
            unit.animator.Play(animationName);

            //always start idle by locking to the closest hostile target
            unit.target = unit.findClosestPlayer();

            //if the target was spotted, turn towards the target
            if(unit.targetSpotted) unit.TurnToTarget();
        }
    }
}
