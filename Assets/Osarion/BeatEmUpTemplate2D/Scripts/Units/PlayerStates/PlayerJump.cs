using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Handles jump arc and transitions for the player.
    //state for player jump
    public class PlayerJump : State {

        private const float LANDING_BUFFER = 0.05f;

        private string animationName = "Jump";
        private string sfxName = "JumpUp";
        private bool hasLanded;
        private int playerId => unit.settings.playerId;
        
        public override void Enter(){
            unit.StopMoving(true);
            unit.animator.Play(animationName);
            unit.StartPhysicalJump();
            AudioController.PlaySFX(sfxName, unit.transform.position);
         }

        public override void Update(){
            //dash in air
            if(InputManager.DashKeyDown(playerId) && unit.IsDashAvailable) {
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
