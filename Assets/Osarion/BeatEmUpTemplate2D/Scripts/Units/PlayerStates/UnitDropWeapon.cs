using UnityEngine;
using System.Collections;

namespace BeatEmUpTemplate2D {

    public class UnitDropWeapon : State {

        private const float THROW_DISTANCE = 1.25f;
        private const float THROW_DEPTH_DISTANCE = 0.6f;
        private const float THROW_ARC_HEIGHT = 0.25f;
        private const float THROW_DURATION = 0.18f;

        private string animationName = "DropWeapon";
        private float animDuration => unit.GetAnimDuration(animationName);
        private float droptime = .5f; //drop at half the time of this animation
        private bool itemThrown;
        private Coroutine throwRoutine;

        public override void Enter(){
            unit.StopMoving();
            unit.animator.Play(animationName);
        }

        public override void Update(){

            //drop weapon at half time
            if(!itemThrown && unit.weapon && (Time.time - stateStartTime) > animDuration * droptime){
                itemThrown = true;
                WeaponPickup weaponToThrow = unit.weapon;
                unit.GetComponentInChildren<WeaponAttachment>()?.DropCurrentWeapon();

                if(weaponToThrow != null) {
                    throwRoutine = unit.StartCoroutine(ThrowWeaponRoutine(weaponToThrow));
                }
            }

            //return to idle when animation is finished
            if((Time.time - stateStartTime) > animDuration){
                unit.stateMachine.SetState(new PlayerIdle()); 
            }
        }

        public override void Exit() {
            if(throwRoutine != null) {
                unit.StopCoroutine(throwRoutine);
                throwRoutine = null;
            }
        }

        private IEnumerator ThrowWeaponRoutine(WeaponPickup thrownWeapon) {
            if(thrownWeapon == null) {
                yield break;
            }

            Vector2 inputVector = InputManager.GetInputVector(unit.settings.playerId);
            Vector3 startPosition = thrownWeapon.transform.position;
            Vector3 forwardOffset = new Vector3((int)unit.dir * THROW_DISTANCE, 0f, inputVector.y * THROW_DEPTH_DISTANCE);
            Vector3 endPosition = startPosition + forwardOffset;
            float t = 0f;

            while(t < 1f && thrownWeapon != null) {
                float arc = Mathf.Sin(Mathf.PI * t) * THROW_ARC_HEIGHT;
                Vector3 targetPosition = Vector3.Lerp(startPosition, endPosition, t);
                targetPosition.y += arc;
                thrownWeapon.transform.position = targetPosition;
                t += Time.deltaTime / THROW_DURATION;
                yield return null;
            }

            if(thrownWeapon != null) {
                thrownWeapon.transform.position = endPosition;
            }
            throwRoutine = null;
        }
    }
}
