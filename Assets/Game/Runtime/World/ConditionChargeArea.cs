// Purpose: Provides test zones that apply condition charge over time while units stay inside a trigger volume.
using UnityEngine;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ConditionChargeArea : MonoBehaviour {

        [Header("Condition Application")]
        public CONDITIONTYPE conditionType = CONDITIONTYPE.BURNING;
        [Range(0f, 1f)] public float chargePerSecond = 0.2f;
        public bool affectPlayers = true;
        public bool affectEnemies = true;

        [Header("Debug")]
        public bool drawDebugRadius = true;
        public Color debugColor = new Color(1f, 0.45f, 0f, 0.5f);

        private Collider _triggerCollider;

        private void Awake() {
            _triggerCollider = GetComponent<Collider>();
            if(_triggerCollider != null) {
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnValidate() {
            chargePerSecond = Mathf.Clamp01(chargePerSecond);
            if(_triggerCollider == null) {
                _triggerCollider = GetComponent<Collider>();
            }
            if(_triggerCollider != null) {
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other) {
            if(conditionType == CONDITIONTYPE.NONE || chargePerSecond <= 0f || other == null) {
                return;
            }

            ConditionManager conditionManager = other.GetComponentInParent<ConditionManager>();
            if(conditionManager == null) {
                return;
            }

            UnitSettings unitSettings = conditionManager.GetComponent<UnitSettings>();
            if(unitSettings == null) {
                return;
            }

            if(unitSettings.unitType == UNITTYPE.PLAYER && !affectPlayers) {
                return;
            }

            if(unitSettings.unitType == UNITTYPE.ENEMY && !affectEnemies) {
                return;
            }

            conditionManager.ApplyConditionChargePerSecond(conditionType, chargePerSecond, gameObject);
        }

        private void OnDrawGizmosSelected() {
            if(!drawDebugRadius) {
                return;
            }

            Collider colliderRef = _triggerCollider != null ? _triggerCollider : GetComponent<Collider>();
            if(colliderRef == null) {
                return;
            }

            Gizmos.color = debugColor;
            Bounds bounds = colliderRef.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}

