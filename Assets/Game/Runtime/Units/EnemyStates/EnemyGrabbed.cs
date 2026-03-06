using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Locks enemy in a grabbed state while attached to player control flow.
    //sets enemy in a grabbed state
    public class EnemyGrabbed : State {

        private string animationName = "Grabbed";
        public override bool canGrab => false; //cannot be grabbed in this state
        private float grabDuration;
        private GameObject player;
        private Vector3 grabPos;

        public EnemyGrabbed(GameObject player, Vector3 grabPos){
            this.player = player;
            this.grabPos = grabPos;
        }

        public override void Enter(){

            //return to idle if there is no player
            if(player == null) {
                unit.stateMachine.SetState(new EnemyIdle());
                return;
            }

            unit.StopMoving();
            unit.animator.Play(animationName);

            //move into grab position
            unit.transform.position = grabPos;
            unit.baseHeight = unit.transform.position.y;
            unit.groundPos = unit.transform.position.z;

            //turn to the opposite direction as the grabber
            unit.TurnToDir((DIRECTION)(-(int)player.GetComponent<UnitActions>().dir));
            
            //duration before grab expires
            grabDuration = player.GetComponent<UnitActions>().settings.grabDuration;
        }

        public override void Update(){

            //grab expires
            if(Time.time - stateStartTime > grabDuration) unit.stateMachine.SetState(new EnemyIdle());
        }
    }
}

