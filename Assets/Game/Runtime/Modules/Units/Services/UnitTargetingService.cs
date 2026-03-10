// Purpose: Centralizes unit target validation and hostile target selection queries.
using UnityEngine;

namespace WOR.Gameplay.Modules.Units.Services {

    public static class UnitTargetingService {

        public static bool IsValidCombatTarget(UnitActions seeker, UnitActions candidate) {
            if(seeker == null || candidate == null || seeker == candidate) {
                return false;
            }

            if(!candidate.gameObject.activeInHierarchy) {
                return false;
            }

            UnitSettings seekerSettings = seeker.settings;
            UnitSettings candidateSettings = candidate.settings;
            if(seekerSettings == null || candidateSettings == null) {
                return false;
            }

            if(!seekerSettings.canDetect || !candidateSettings.canBeDetected) {
                return false;
            }

            bool seekerIsConfused = seekerSettings.faction == UNITFACTION.CONFUSED;
            if(!seekerIsConfused && candidateSettings.faction == seekerSettings.faction) {
                return false;
            }

            HealthSystem healthSystem = candidate.GetComponent<HealthSystem>();
            if(healthSystem != null && healthSystem.isDead) {
                return false;
            }

            return true;
        }

        public static GameObject FindClosestHostile(UnitActions seeker) {
            if(seeker == null || seeker.settings == null || !seeker.settings.canDetect) {
                return null;
            }

            float closestSqrDistance = float.MaxValue;
            UnitActions closestTarget = null;

            float seekerDepth = seeker.groundPos;
            float seekerX = seeker.transform.position.x;

            foreach(UnitActions candidate in UnitRegistryService.GetRegisteredUnits()) {
                if(!IsValidCombatTarget(seeker, candidate)) {
                    continue;
                }

                float deltaX = candidate.transform.position.x - seekerX;
                float deltaZ = candidate.groundPos - seekerDepth;
                float sqrDistance = deltaX * deltaX + deltaZ * deltaZ;

                if(sqrDistance >= closestSqrDistance) {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestTarget = candidate;
            }

            return closestTarget != null ? closestTarget.gameObject : null;
        }

        public static GameObject FindNearbyDownedHostile(UnitActions seeker, float range) {
            if(seeker == null || range <= 0f) {
                return null;
            }

            float rangeSqr = range * range;
            Vector3 seekerPosition = seeker.transform.position;

            foreach(UnitActions candidate in UnitRegistryService.GetRegisteredUnits()) {
                if(!IsValidCombatTarget(seeker, candidate)) {
                    continue;
                }

                Vector3 candidatePosition = candidate.transform.position;
                float sqrDistance = (candidatePosition - seekerPosition).sqrMagnitude;
                if(sqrDistance > rangeSqr) {
                    continue;
                }

                StateMachine stateMachine = candidate.stateMachine;
                if(stateMachine == null) {
                    continue;
                }

                if(stateMachine.GetCurrentState() is UnitKnockDownGrounded) {
                    return candidate.gameObject;
                }
            }

            return null;
        }
    }
}
