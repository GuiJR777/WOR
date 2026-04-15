// Purpose: Automatically ensures all SpriteRenderers in loaded scenes have SpriteBillboard.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WOR.Gameplay {

    [DefaultExecutionOrder(-10000)]
    public class SpriteBillboardInstaller : MonoBehaviour {

        private const float RESCAN_INTERVAL = 0.75f;
        private static SpriteBillboardInstaller _instance;
        private float _nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if(_instance != null) {
                return;
            }

            GameObject installerObject = new GameObject(nameof(SpriteBillboardInstaller));
            DontDestroyOnLoad(installerObject);
            _instance = installerObject.AddComponent<SpriteBillboardInstaller>();
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureBillboardOnAllSprites(FindObjectsInactive.Include);
            _nextScanTime = Time.unscaledTime + RESCAN_INTERVAL;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update() {
            if(Time.unscaledTime < _nextScanTime) {
                return;
            }

            EnsureBillboardOnAllSprites(FindObjectsInactive.Exclude);
            _nextScanTime = Time.unscaledTime + RESCAN_INTERVAL;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
            EnsureBillboardOnAllSprites(FindObjectsInactive.Include);
        }

        private static void EnsureBillboardOnAllSprites(FindObjectsInactive includeInactive) {
            SpriteRenderer[] allSprites = Object.FindObjectsByType<SpriteRenderer>(includeInactive, FindObjectsSortMode.None);
            for(int i = 0; i < allSprites.Length; i++) {
                SpriteRenderer spriteRenderer = allSprites[i];
                if(spriteRenderer == null) {
                    continue;
                }

                SpriteBillboard existingBillboard = spriteRenderer.GetComponent<SpriteBillboard>();
                bool ignoredByMarker = spriteRenderer.GetComponentInParent<SpriteBillboardIgnore>() != null;
                if(ignoredByMarker) {
                    if(existingBillboard != null) {
                        Object.Destroy(existingBillboard);
                    }
                    continue;
                }

                if(existingBillboard != null) {
                    continue;
                }

                SpriteBillboard billboard = spriteRenderer.gameObject.AddComponent<SpriteBillboard>();
                billboard.SetAxisMode(BILLBOARDAXISMODE.X);
            }
        }
    }
}


