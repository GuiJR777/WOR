// Purpose: Spawns floating damage numbers for every damage event broadcast by HealthSystem.
using UnityEngine;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    public class DamageNumberRuntimeSpawner : MonoBehaviour {

        private const float DEFAULT_VERTICAL_OFFSET = 0.9f;
        private const float DEFAULT_RANDOM_X_OFFSET = 0.2f;
        private const float DEFAULT_TEXT_CHARACTER_SIZE = 0.035f;
        private const int DEFAULT_TEXT_FONT_SIZE = 64;
        private const int DEFAULT_TEXT_SORTING_ORDER = 400;

        private static DamageNumberRuntimeSpawner _instance;

        [Header("Spawn")]
        public float verticalOffset = DEFAULT_VERTICAL_OFFSET;
        public float randomXOffset = DEFAULT_RANDOM_X_OFFSET;

        [Header("Visual")]
        public float characterSize = DEFAULT_TEXT_CHARACTER_SIZE;
        public int fontSize = DEFAULT_TEXT_FONT_SIZE;
        public int sortingOrder = DEFAULT_TEXT_SORTING_ORDER;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if(_instance != null) {
                return;
            }

            GameObject runtimeSpawner = new GameObject("DamageNumberRuntimeSpawner");
            DontDestroyOnLoad(runtimeSpawner);
            _instance = runtimeSpawner.AddComponent<DamageNumberRuntimeSpawner>();
        }

        private void OnEnable() {
            HealthSystem.onUnitDamaged += HandleUnitDamaged;
        }

        private void OnDisable() {
            HealthSystem.onUnitDamaged -= HandleUnitDamaged;
        }

        private void HandleUnitDamaged(HealthSystem healthSystem, int damageAmount, bool isConditionDamage, Color damageColor) {
            if(healthSystem == null || damageAmount <= 0) {
                return;
            }

            Vector3 spawnPosition = healthSystem.transform.position + new Vector3(
                Random.Range(-randomXOffset, randomXOffset),
                verticalOffset,
                0f);

            GameObject damageTextObject = new GameObject($"Damage_{damageAmount}");
            damageTextObject.transform.position = spawnPosition;

            TextMesh textMesh = damageTextObject.AddComponent<TextMesh>();
            textMesh.text = damageAmount.ToString();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = isConditionDamage ? damageColor : Color.white;

            MeshRenderer textRenderer = damageTextObject.GetComponent<MeshRenderer>();
            if(textRenderer != null) {
                textRenderer.sortingOrder = sortingOrder;
            }

            DamageNumberFloat motion = damageTextObject.AddComponent<DamageNumberFloat>();
            motion.Initialize(textMesh.color);
        }
    }
}

