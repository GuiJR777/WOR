// Purpose: Aligns sprite transforms to camera orientation on selected axes.
using UnityEngine;

namespace BeatEmUpTemplate2D {

    public enum BILLBOARDAXISMODE {
        X = 0,
        Y = 1,
        Z = 2,
        XY = 3,
        XZ = 4,
        YZ = 5,
        XYZ = 6,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBillboard : MonoBehaviour {

        [SerializeField] private BILLBOARDAXISMODE axisMode = BILLBOARDAXISMODE.X;
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 eulerOffset;
        [SerializeField] private bool preserveUnitFacing = true;

        private UnitActions _unitActions;

        private void Awake() {
            _unitActions = GetComponent<UnitActions>();
        }

        private void LateUpdate() {
            Camera cameraToUse = ResolveCamera();
            if(cameraToUse == null) {
                return;
            }

            Vector3 currentEuler = GetStableCurrentEuler();
            Vector3 targetEuler = cameraToUse.transform.rotation.eulerAngles + eulerOffset;
            targetEuler = ApplyFacingCompensation(currentEuler, targetEuler);
            transform.rotation = Quaternion.Euler(ApplyAxisMode(currentEuler, targetEuler));
        }

        public void SetAxisMode(BILLBOARDAXISMODE mode) {
            axisMode = mode;
        }

        private Camera ResolveCamera() {
            if(!useMainCamera && targetCamera != null) {
                return targetCamera;
            }
            return Camera.main;
        }

        private Vector3 GetStableCurrentEuler() {
            Vector3 currentEuler = transform.rotation.eulerAngles;
            if(!preserveUnitFacing || _unitActions == null) {
                return currentEuler;
            }

            // Preserve unit facing explicitly to avoid Euler ambiguity when Y = 180.
            currentEuler.y = _unitActions.dir == DIRECTION.LEFT ? 180f : 0f;
            currentEuler.z = 0f;
            return currentEuler;
        }

        private Vector3 ApplyFacingCompensation(Vector3 currentEuler, Vector3 targetEuler) {
            if(!preserveUnitFacing || !AxisIncludesX(axisMode)) {
                return targetEuler;
            }

            float facingSign = GetFacingSign(currentEuler);
            if(facingSign >= 0f) {
                return targetEuler;
            }

            // When facing left (Y=180), invert X billboard tilt (e.g. 30 -> -30).
            float signedTargetX = Mathf.DeltaAngle(0f, targetEuler.x);
            targetEuler.x = -signedTargetX;
            return targetEuler;
        }

        private float GetFacingSign(Vector3 currentEuler) {
            if(_unitActions != null) {
                return _unitActions.dir == DIRECTION.LEFT ? -1f : 1f;
            }

            float normalizedY = Mathf.Repeat(currentEuler.y, 360f);
            bool facingLeft = normalizedY > 90f && normalizedY < 270f;
            return facingLeft ? -1f : 1f;
        }

        private static bool AxisIncludesX(BILLBOARDAXISMODE mode) {
            return mode == BILLBOARDAXISMODE.X
                || mode == BILLBOARDAXISMODE.XY
                || mode == BILLBOARDAXISMODE.XZ
                || mode == BILLBOARDAXISMODE.XYZ;
        }

        private Vector3 ApplyAxisMode(Vector3 currentEuler, Vector3 targetEuler) {
            switch(axisMode) {
                case BILLBOARDAXISMODE.X:
                    currentEuler.x = targetEuler.x;
                    break;
                case BILLBOARDAXISMODE.Y:
                    currentEuler.y = targetEuler.y;
                    break;
                case BILLBOARDAXISMODE.Z:
                    currentEuler.z = targetEuler.z;
                    break;
                case BILLBOARDAXISMODE.XY:
                    currentEuler.x = targetEuler.x;
                    currentEuler.y = targetEuler.y;
                    break;
                case BILLBOARDAXISMODE.XZ:
                    currentEuler.x = targetEuler.x;
                    currentEuler.z = targetEuler.z;
                    break;
                case BILLBOARDAXISMODE.YZ:
                    currentEuler.y = targetEuler.y;
                    currentEuler.z = targetEuler.z;
                    break;
                case BILLBOARDAXISMODE.XYZ:
                    currentEuler = targetEuler;
                    break;
            }
            return currentEuler;
        }
    }
}
