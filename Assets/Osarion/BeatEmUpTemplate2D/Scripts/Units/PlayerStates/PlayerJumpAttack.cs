using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Executes attack logic while player is airborne.
    //state for attacking during a jump
    public class PlayerJumpAttack : State {

        private const float LANDING_BUFFER = 0.05f;

        private AttackData attackData;
        private bool damageDealt;
        private bool hasLanded;
        private int playerId => unit.settings.playerId;
    
        public PlayerJumpAttack(AttackData attackData){
            this.attackData = attackData;
        }
    
        public override void Enter(){
            unit.isGrounded = false;
            unit.animator.Play(attackData.animationState);
        }

        public override void Update(){
            //dash in air
            if(InputManager.DashKeyDown(playerId) && unit.IsDashAvailable) {
                unit.stateMachine.SetState(new PlayerDash());
                return;
            }

            //go to landed state
            if(hasLanded) unit.stateMachine.SetState(new PlayerLand());

            //check for hit
            if(!damageDealt) damageDealt = unit.CheckForHit(attackData); 
        }

        public override void FixedUpdate(){

            //preform jump
            unit.JumpSequence();

            //end of jump when body touches the ground again
            if(!hasLanded && (Time.time - stateStartTime) > LANDING_BUFFER && unit.isGrounded){
                hasLanded = true;
            }
        }
    }
}
