using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Keeps legacy sorting API as a no-op for the new 3D depth workflow.
    public class ObjectSorting : MonoBehaviour {

        public static int SortingStep = -50;

        public virtual void Start() {
        }

        public static void Sort(Renderer rend, Vector2 position) {
        }

        private void OnValidate() {
        }
    }
}
