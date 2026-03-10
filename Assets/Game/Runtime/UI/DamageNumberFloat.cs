// Purpose: Animates floating damage numbers upward with fade-out while facing the active camera.
using UnityEngine;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMesh))]
    public class DamageNumberFloat : MonoBehaviour {

        private const float DEFAULT_LIFETIME = 0.75f;
        private const float DEFAULT_FLOAT_SPEED = 0.95f;

        [Min(0.05f)] public float lifetime = DEFAULT_LIFETIME;
        [Min(0.01f)] public float floatSpeed = DEFAULT_FLOAT_SPEED;

        private TextMesh _textMesh;
        private Color _initialColor;
        private float _spawnTime;
        private Camera _cachedMainCamera;

        private void Awake() {
            _textMesh = GetComponent<TextMesh>();
            _initialColor = _textMesh != null ? _textMesh.color : Color.white;
            _spawnTime = Time.time;
        }

        public void Initialize(Color color) {
            if(_textMesh == null) {
                _textMesh = GetComponent<TextMesh>();
            }

            if(_textMesh != null) {
                _textMesh.color = color;
                _initialColor = color;
            }
        }

        private void Update() {
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            float elapsed = Time.time - _spawnTime;
            float normalizedLifetime = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;
            float alpha = 1f - normalizedLifetime;

            if(_textMesh != null) {
                Color textColor = _initialColor;
                textColor.a = alpha;
                _textMesh.color = textColor;
            }

            if(normalizedLifetime >= 1f) {
                Destroy(gameObject);
            }
        }

        private void LateUpdate() {
            if(_cachedMainCamera == null || !_cachedMainCamera.isActiveAndEnabled) {
                _cachedMainCamera = Camera.main;
            }

            if(_cachedMainCamera != null) {
                transform.rotation = _cachedMainCamera.transform.rotation;
            }
        }
    }
}

