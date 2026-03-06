using UnityEngine;

namespace BeatEmUpTemplate2D {

    //class for throwing an enemy
    public class PlayerThrowEnemy : State {

        private const float THROW_DIRECTION_INPUT_THRESHOLD = 0.75f;
        private string animationName = "GrabThrow";
        private float animDuration => unit.GetAnimDuration(animationName);
        private GameObject enemy;
        private int playerId => unit.settings != null ? unit.settings.playerId : 1;
        private DIRECTION launchDirection = DIRECTION.RIGHT;

        public PlayerThrowEnemy(GameObject enemy){
            this.enemy = enemy;
        }

        public override void Enter(){

            //return to idle if there is no enemy
            if(!enemy) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }
            unit.StopMoving();
            unit.animator.Play(animationName);

            //default throw behavior is backward relative to current facing
            DIRECTION currentDir = unit.dir; //current player direction
            DIRECTION backwardDir = OppositeDirection(currentDir);
            launchDirection = ResolveThrowDirection(backwardDir);
            unit.TurnToDir(launchDirection);

            //move enemy to position of the player
            enemy.transform.position = unit.transform.position;

            //turn enemy opposite to launch direction for better throw readability
            UnitActions ua = enemy.GetComponent<UnitActions>();
            ua?.TurnToDir(OppositeDirection(launchDirection));

            //use knockdown state for throw
            UnitSettings enemySettings = enemy.GetComponent<UnitSettings>();
            AttackData throwAttackData = unit.settings.grabThrow;
            float knockdownHorizontalForce = throwAttackData.knockdownLaunchHorizontalForce > 0f
                ? throwAttackData.knockdownLaunchHorizontalForce
                : enemySettings.throwDistance;
            float knockdownVerticalForce = throwAttackData.knockdownLaunchVerticalForce > 0f
                ? throwAttackData.knockdownLaunchVerticalForce
                : enemySettings.throwHeight;
            throwAttackData.inflictor = unit.gameObject;
            enemy.GetComponent<StateMachine>()?.SetState(
                new UnitKnockDown(throwAttackData, knockdownHorizontalForce, knockdownVerticalForce, (int)launchDirection));
        }

        public override void Update(){
            unit.TurnToDir(launchDirection);
            if((Time.time - stateStartTime) > animDuration) unit.stateMachine.SetState(new PlayerIdle()); //return to idle when animation is finished
        }

        private DIRECTION ResolveThrowDirection(DIRECTION fallbackDirection) {
            Vector2 inputVector = InputManager.GetInputVector(playerId);
            if(Mathf.Abs(inputVector.x) < THROW_DIRECTION_INPUT_THRESHOLD) {
                return fallbackDirection;
            }
            return inputVector.x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT;
        }

        private static DIRECTION OppositeDirection(DIRECTION direction) {
            return direction == DIRECTION.LEFT ? DIRECTION.RIGHT : DIRECTION.LEFT;
        }
    }
}
