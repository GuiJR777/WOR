// Purpose: Triggers ControledByAI for players and commands pathfinding movement to a target transform.
using UnityEngine;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PlayerAiControlZone : MonoBehaviour {

        private const float MinStoppingDistance = 0.01f;

        [Header("Target")]
        public Transform targetPoint;
        [Min(MinStoppingDistance)] public float stoppingDistance = 0.1f;

        [Header("Trigger Behavior")]
        public bool triggerOnlyOnce = true;
        public bool disableAfterTrigger = true;

        [Header("Debug")]
        public bool drawDebugBounds = true;
        public Color debugColor = new Color(0.2f, 0.8f, 1f, 0.4f);

        private Collider _triggerCollider;
        private bool _alreadyTriggered;

        private void Awake() {
            EnsureTriggerCollider();
        }

        private void OnValidate() {
            stoppingDistance = Mathf.Max(MinStoppingDistance, stoppingDistance);
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other) {
            if(other == null) {
                return;
            }

            if(triggerOnlyOnce && _alreadyTriggered) {
                return;
            }

            if(targetPoint == null) {
                Debug.LogWarning($"{nameof(PlayerAiControlZone)} requires a target point transform.", this);
                return;
            }

            UnitActions unitActions = other.GetComponentInParent<UnitActions>();
            if(unitActions == null || !unitActions.isPlayer) {
                return;
            }

            StateMachine stateMachine = unitActions.stateMachine;
            if(stateMachine == null) {
                return;
            }

            stateMachine.SetControledByAiState(targetPoint, stoppingDistance);

            _alreadyTriggered = true;
            if(disableAfterTrigger) {
                DisableZone();
            }
        }

        private void OnDrawGizmosSelected() {
            if(!drawDebugBounds) {
                return;
            }

            Collider colliderRef = _triggerCollider != null ? _triggerCollider : GetComponent<Collider>();
            if(colliderRef == null) {
                return;
            }

            Gizmos.color = debugColor;
            Bounds bounds = colliderRef.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if(targetPoint != null) {
                Gizmos.DrawLine(bounds.center, targetPoint.position);
                Gizmos.DrawSphere(targetPoint.position, 0.12f);
            }
        }

        private void EnsureTriggerCollider() {
            if(_triggerCollider == null) {
                _triggerCollider = GetComponent<Collider>();
            }

            if(_triggerCollider != null) {
                _triggerCollider.isTrigger = true;
            }
        }

        private void DisableZone() {
            if(_triggerCollider != null) {
                _triggerCollider.enabled = false;
            }

            enabled = false;
        }
    }
}
