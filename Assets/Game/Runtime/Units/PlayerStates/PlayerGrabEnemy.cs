using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Handles player control flow while holding an enemy grab.
    //ctate for actions when the player is holding an enemy in a grab.
    public class PlayerGrabEnemy : State {

        private GameObject enemy;
        private int playerId => unit.settings.playerId;

        public PlayerGrabEnemy(GameObject enemy){
            this.enemy = enemy;
        }

        public override void Enter(){
            if(enemy == null){
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            unit.StopMoving(true);
            if(!string.IsNullOrEmpty(unit.settings.grabAnimation)){
                unit.animator.Play(unit.settings.grabAnimation, 0, 0f);
            }

            //calculate grab position
            Vector3 grabOffset = new Vector3(
                unit.settings.grabPosition.x * (int)unit.dir,
                0f,
                unit.settings.grabPosition.y);
            Vector3 enemyGrabPos = new Vector3(
                unit.transform.position.x + grabOffset.x,
                unit.baseHeight,
                unit.groundPos + grabOffset.z);

            //put enemy in grab state
            enemy.GetComponent<StateMachine>()?.SetState(new EnemyGrabbed(unit.gameObject, enemyGrabPos));
        }

        public override void Update(){
             unit.groundPos = unit.transform.position.z;

            //punch button was pressed during grab
            if(InputManager.PunchKeyDown(playerId)){
                unit.stateMachine.SetState(new PlayerGrabAttack(unit.settings.grabPunch));
                enemy = null;
                return;
            }

            //kick button was pressed during grab
            if(InputManager.KickKeyDown(playerId)){
                unit.stateMachine.SetState(new PlayerGrabAttack(unit.settings.grabKick));
                enemy = null;
                return;
            }

            //throw button was pressed during grab
            if(InputManager.GrabKeyDown(playerId)){
                unit.stateMachine.SetState(new PlayerThrowEnemy(enemy));
                enemy = null;
                return;
            }

            //release grab when time expires
            if(Time.time - stateStartTime > unit.settings.grabDuration){
                unit.stateMachine.SetState(new PlayerIdle()); //player return to Idle
                enemy = null;
            }
        }

        public override void Exit(){
            //release enemy from grab if something else happens
            if(enemy != null) enemy.GetComponent<StateMachine>()?.SetState(new EnemyIdle());
        }
    }
}

