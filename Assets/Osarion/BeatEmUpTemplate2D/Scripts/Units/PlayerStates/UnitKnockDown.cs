using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Simulates airborne knockdown arc, bounce and grounded transition.
    //class for units during KnockDown
    public class UnitKnockDown : State {

        private string animationNameUp = "KnockDown Up";
        private string animationNameDown = "KnockDown Down";
        public override bool canGrab => false; //unit cannot be grabbed in this state
        private AttackData attackData;
        private int bounceNum = 1;    
        private float xForce;
        private float yForce;
        private bool fallDamageApplied; //set to true when this unit takes damage upon hitting the floor
        private bool hasHitFloor; //set to true when this unit has hit the the floor once

        public UnitKnockDown(AttackData _attackData, float xForce, float yForce){
            attackData = _attackData;
            this.xForce = xForce;
            this.yForce = yForce;
        }
   
        public override void Enter(){
            unit.StopMoving();
            unit.isGrounded = false;
            unit.baseHeight = unit.transform.position.y;
            unit.SetGravityEnabled(false);
            
            //turn towards direction of the player attack
            if(attackData.inflictor != null && attackData.inflictor.CompareTag("Player")) unit.TurnToDir((DIRECTION)((int)attackData.inflictor.GetComponent<UnitActions>().dir * -1)); 
        
            //lose equipped weapon
            if(unit.settings.loseWeaponWhenKnockedDown && unit.weapon != null){
                unit.GetComponentInChildren<WeaponAttachment>()?.LoseCurrentWeapon();
            }
        }

        public override void Update(){
            Vector3 moveVector = unit.transform.position;

            //x force
            moveVector.x = unit.transform.position.x + -(int)unit.dir * xForce * Time.deltaTime;
            xForce = xForce>0? xForce -= Time.deltaTime : 0; //decrease over time

            //hit other units when falling down (optional)
            bool goingDown = yForce < 0;
            bool hurtOtherEnemiesWhenThrown = unit.settings.hitOtherEnemiesWhenThrown && attackData.attackType == ATTACKTYPE.GRABTHROW;
            bool hurtOtherEnemiesWhenFalling = unit.settings.hitOtherEnemiesWhenFalling;
            if(!hasHitFloor && goingDown && (hurtOtherEnemiesWhenThrown || hurtOtherEnemiesWhenFalling)) unit.CheckForHit(attackData);

            //y force (bounce up until there are no more bounces left)
            if(unit.transform.position.y < unit.baseHeight){
                hasHitFloor = true;

                //if this is a GRABTHROW attack, apply fall damage when this unit hits the floor
                if(!fallDamageApplied && attackData.attackType == ATTACKTYPE.GRABTHROW) {
                    unit.GetComponent<HealthSystem>()?.SubstractHealth(attackData.damage);
                    fallDamageApplied = true;
                }

                //keep bouncing until there are no more bounces left
                if(bounceNum>0){
                    unit.transform.position = new Vector3(unit.transform.position.x, unit.baseHeight, unit.transform.position.z); //position this unit on the floor 
                    yForce = unit.settings.knockDownHeight/2f; //add force but make the next bounce less high
                    unit.CamShake();
                    bounceNum --;
                    return;

                } else {

                    //unit has landed on the floor
                    unit.stateMachine.SetState(new UnitKnockDownGrounded());
                    return;
                }
            }

            //vertical movement
            moveVector.y += yForce * Time.deltaTime * unit.settings.knockDownSpeed;
            yForce -= unit.settings.jumpGravity * Time.deltaTime * unit.settings.knockDownSpeed;

            //move unit
            unit.transform.position = moveVector;

            //play up/down animation
            if(yForce>0) unit.animator.Play(animationNameUp);
            else unit.animator.Play(animationNameDown);
        }

        public override void Exit(){

            //make sure this unit is on the floor again when exiting this state
            Vector3 position = unit.transform.position;
            unit.transform.position = new Vector3(position.x, unit.baseHeight, unit.groundPos);
            unit.isGrounded = true;
            unit.SetGravityEnabled(true);
        }
    }
}
