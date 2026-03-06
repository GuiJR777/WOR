// Purpose: Stores enemy AI runtime state independent from MonoBehaviour lifecycle.
using UnityEngine;
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.AI.Models {

    public sealed class EnemyAiModel : IModel {

        public float StartTime { get; private set; }
        public float LastDecisionTime { get; private set; }
        public bool TargetSpotted { get; private set; }

        public void Initialize(float startTime) {
            StartTime = startTime;
            LastDecisionTime = 0f;
            TargetSpotted = false;
        }

        public bool HasStartDelayElapsed(float timeNow, float delayBeforeStart) {
            return timeNow - StartTime >= Mathf.Max(0f, delayBeforeStart);
        }

        public bool CanMakeDecision(float timeNow, float decisionInterval) {
            return timeNow - LastDecisionTime > Mathf.Max(0f, decisionInterval);
        }

        public void MarkDecisionTaken(float decisionTimestamp) {
            LastDecisionTime = decisionTimestamp;
        }

        public void SetTargetSpotted(bool spotted) {
            TargetSpotted = spotted;
        }
    }
}
