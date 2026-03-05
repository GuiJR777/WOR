using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Handles defend behavior, including parry window resolution.
    public class UnitDefend : State {

        private string animationName = "Defend";
        private string defendHitSFX = "DefendHit";
        private string parrySFX = "DefendHit";
        private float enemyDefendDuration => unit.settings.defendDuration;
        private int playerId => unit.settings.playerId;
        private bool parryConsumed;
    
        public override void Enter(){
            unit.animator.Play(animationName);
            unit.StopMoving();
        }

        public override void Update(){

            //for players
            if(unit.isPlayer){

                //change direction while defening
                if(unit.isPlayer && unit.settings.canChangeDirWhileDefending){
                    Vector2 inputVector = InputManager.GetInputVector(playerId);
                    if(inputVector.x == 1) unit.TurnToDir(DIRECTION.RIGHT);
                    else if(inputVector.x == -1) unit.TurnToDir(DIRECTION.LEFT);
                }

                //return to idle when defend button is released
                if(!InputManager.DefendKeyDown(playerId)) unit.stateMachine.SetState(new PlayerIdle());
            }

            //for enemies
            if(unit.isEnemy && (Time.time - stateStartTime > enemyDefendDuration)) unit.stateMachine.SetState(new EnemyIdle());
        }

        public bool TryParry(UnitActions attacker, AttackData attackData){
            if(attacker == null || unit.settings == null || parryConsumed) return false;
            if(unit.settings.parryWindow <= 0f) return false;
            if(Time.time - stateStartTime > unit.settings.parryWindow) return false;

            parryConsumed = true;
            unit.ShowEffect("DefendEffect");
            AudioController.PlaySFX(parrySFX, unit.transform.position);
            attacker.OnParried(unit);
            unit.CamShake();
            return true;
        }

        //we are hit while defending
        public void Hit(){
           
            //show defend effect
            unit.ShowEffect("DefendEffect"); 

            //play sfx
            BeatEmUpTemplate2D.AudioController.PlaySFX(defendHitSFX, unit.transform.position);
        }
    }

    // Purpose: Applies a temporary stun lock after parry or scripted interruptions.
    public class UnitStunned : State {

        private const string STUN_ANIMATION = "Hit";
        private readonly float _stunDuration;

        public override bool canGrab => false;

        public UnitStunned(float stunDuration) {
            _stunDuration = stunDuration;
        }

        public override void Enter() {
            unit.StopMoving(true);
            unit.animator.Play(STUN_ANIMATION, 0, 0f);
        }

        public override void Update() {
            if(Time.time - stateStartTime < _stunDuration) {
                return;
            }

            if(unit.isPlayer) {
                unit.stateMachine.SetState(new PlayerIdle());
            } else {
                unit.stateMachine.SetState(new EnemyIdle());
            }
        }
    }
}
