using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Handles jump arc and transitions for the player.
    //state for player jump
    public class PlayerJump : State {

        private const float LANDING_BUFFER = 0.05f;

        private string animationName = "Jump";
        private string sfxName = "JumpUp";
        private bool hasLanded;
        private readonly bool applyJumpImpulse;
        private int playerId => unit.settings.playerId;

        public PlayerJump(bool applyJumpImpulse = true) {
            this.applyJumpImpulse = applyJumpImpulse;
        }
        
        public override void Enter(){
            if(applyJumpImpulse && unit.HasActiveCondition(CONDITIONTYPE.SOAKED)) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            unit.animator.Play(animationName);
            if(applyJumpImpulse) {
                unit.StopMoving(true);
                unit.StartPhysicalJump();
                AudioController.PlaySFX(sfxName, unit.transform.position);
            } else {
                unit.isGrounded = false;
            }
         }

        public override void Update(){
            //dash in air
            if(!unit.HasActiveCondition(CONDITIONTYPE.SOAKED) && InputManager.DashKeyDown(playerId) && unit.IsDashAvailable) {
                unit.stateMachine.SetState(new PlayerDash());
                return;
            }

            //perform jump punch attack
            if(InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerJumpAttack(unit.settings.jumpPunch)); return; }

            //perform jump kick attack
            if(InputManager.KickKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerJumpAttack(unit.settings.jumpKick)); return; }

            //go to landed state
            if(hasLanded) unit.stateMachine.SetState(new PlayerLand());
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

