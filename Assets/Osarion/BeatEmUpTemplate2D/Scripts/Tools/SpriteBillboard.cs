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

        private void LateUpdate() {
            Camera cameraToUse = ResolveCamera();
            if(cameraToUse == null) {
                return;
            }

            Vector3 currentEuler = transform.rotation.eulerAngles;
            Vector3 targetEuler = cameraToUse.transform.rotation.eulerAngles + eulerOffset;
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

