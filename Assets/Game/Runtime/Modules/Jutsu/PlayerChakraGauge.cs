// Purpose: Tracks chakra as fillable bars and supports hit-based gain plus bar consumption by jutsu casts.
using UnityEngine;

namespace WOR.Gameplay.Modules.Jutsu {

    [DisallowMultipleComponent]
    public class PlayerChakraGauge : MonoBehaviour {

        private const float PercentPerBar = 100f;
        private const int MinBars = 1;

        [Header("Chakra Bars")]
        [Min(MinBars)] public int maxBars = MinBars;
        [ReadOnlyProperty] public float currentChakraPercent;

        [Header("Hit Gain")]
        [Range(0f, PercentPerBar)] public float baseGainPercentPerHit = 10f;
        [Range(0f, PercentPerBar)] public float upgradeBonusGainPercentPerHit;

        public float MaxChakraPercent => Mathf.Max(MinBars, maxBars) * PercentPerBar;
        public int FilledBars => Mathf.FloorToInt(currentChakraPercent / PercentPerBar);
        public float GainPercentPerHit => Mathf.Max(0f, baseGainPercentPerHit + upgradeBonusGainPercentPerHit);

        private void Awake() {
            ClampConfiguration();
            currentChakraPercent = 0f;
        }

        private void OnValidate() {
            ClampConfiguration();
        }

        public void AddHitCharge() {
            AddChakraPercent(GainPercentPerHit);
        }

        public void AddChakraPercent(float percentAmount) {
            if(percentAmount <= 0f) {
                return;
            }

            currentChakraPercent = Mathf.Clamp(
                currentChakraPercent + percentAmount,
                0f,
                MaxChakraPercent);
        }

        public bool TryConsumeBars(int barCost) {
            if(barCost <= 0) {
                return true;
            }

            float chakraCost = barCost * PercentPerBar;
            if(currentChakraPercent < chakraCost) {
                return false;
            }

            currentChakraPercent -= chakraCost;
            return true;
        }

        public void SetMaxBars(int newMaxBars, bool preserveRatio = true) {
            float previousMax = MaxChakraPercent;
            float ratio = previousMax > 0f ? Mathf.Clamp01(currentChakraPercent / previousMax) : 0f;

            maxBars = Mathf.Max(MinBars, newMaxBars);
            currentChakraPercent = preserveRatio ? ratio * MaxChakraPercent : 0f;
            currentChakraPercent = Mathf.Clamp(currentChakraPercent, 0f, MaxChakraPercent);
        }

        public void ResetChakraToEmpty() {
            currentChakraPercent = 0f;
        }

        private void ClampConfiguration() {
            maxBars = Mathf.Max(MinBars, maxBars);
            baseGainPercentPerHit = Mathf.Clamp(baseGainPercentPerHit, 0f, PercentPerBar);
            upgradeBonusGainPercentPerHit = Mathf.Clamp(upgradeBonusGainPercentPerHit, 0f, PercentPerBar);
            currentChakraPercent = Mathf.Clamp(currentChakraPercent, 0f, MaxChakraPercent);
        }
    }
}

