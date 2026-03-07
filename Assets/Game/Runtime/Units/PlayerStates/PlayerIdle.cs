using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Handles idle player transitions and input routing.
    public class PlayerIdle : State {

        private const float PICKUP_RANGE = 0.4f;

        private string animationName = "Idle";
        private int playerId => unit.settings.playerId;

        public override void Enter(){
            unit.animator.Play(animationName);
        }

        public override void Update(){

            //stop moving
            unit.StopMoving(false);

            //dash (dash action button)
            if(InputManager.DashKeyDown(playerId) && unit.IsDashAvailable){ unit.stateMachine.SetState(new PlayerDash()); return; }

            //defend
            if(InputManager.DefendKeyDown(playerId)){ unit.stateMachine.SetState(new UnitDefend()); return; }

            //jump
            if(unit.isGrounded && InputManager.JumpKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerJump()); return; }

            //use weapon
            if(unit.weapon && InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerWeaponAttack()); return; }

            //check for nearby enemy to ground Punch
            if(InputManager.PunchKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundPunch()); return; }

            //check for nearby enemy to ground kick
            if(InputManager.KickKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundKick()); return; }

            //punch Key pressed
            if(InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.PUNCH)); return; }

            //kick Key pressed
            if(InputManager.KickKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.KICK)); return; }

            //grab button: pick up nearby item or throw currently equipped item
            if(InputManager.GrabKeyDown(playerId) && !unit.weapon){
                GameObject pickup = unit.GetClosestPickup(Vector2.one * PICKUP_RANGE);
                if(pickup != null){
                    unit.stateMachine.SetState(new PlayerGrabItem(pickup));
                    return;
                }
            }

            if(InputManager.GrabKeyDown(playerId) && unit.weapon){ unit.stateMachine.SetState(new UnitDropWeapon()); return; }

            //move
            if(InputManager.GetInputVector(playerId).magnitude > 0) unit.stateMachine.SetState(new PlayerMove());
        }
    }
}


