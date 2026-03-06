// Purpose: Forces all player state machines into an inactive state after an optional delay.
using System.Collections;
using UnityEngine;

namespace WOR.Gameplay {

    public class UISetPlayerInactive : MonoBehaviour {

        public float startDelay = 3f;

        private void OnEnable() {
            StartCoroutine(SetPlayerInactive(startDelay));
        }

        // Set all player(s) to inactive state.
        private IEnumerator SetPlayerInactive(float delay) {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));

            StateMachine[] stateMachines = GameObject.FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
            for(int i = 0; i < stateMachines.Length; i++) {
                StateMachine unitStateMachine = stateMachines[i];
                if(unitStateMachine == null || unitStateMachine.Unit == null || unitStateMachine.Unit.settings == null) {
                    continue;
                }

                if(unitStateMachine.Unit.settings.unitType == UNITTYPE.PLAYER) {
                    unitStateMachine.SetState(new PlayerInActive());
                }
            }
        }
    }
}
