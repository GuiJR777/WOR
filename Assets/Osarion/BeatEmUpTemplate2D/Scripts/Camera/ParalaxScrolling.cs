using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Applies parallax movement from camera displacement on x and depth axes.
    //class for creating a parallax scrolling effect
    public class ParalaxScrolling : MonoBehaviour {
	
	    public float ParallaxScale = -.3f; //the amount off parallax
	    public float xOffset = 0; //additional x offset
	    public float yOffset = 0; //additional y offset
        public bool useX = true; //disable/enable the X Axis;
        public bool useY = true; //disable/enable the Y Axis;

        private Vector2 prevPos = Vector2.zero;
        private float xPos = 0;
        private float zPos = 0;

        void Start() {
            if(Camera.main != null) prevPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
        }

	    void Update() {
            if(!useX && !useY || !Camera.main) return;
		    Vector2 currentCameraPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
            Vector2 diff = currentCameraPos - prevPos;

            xPos = useX? (diff.x * ParallaxScale) + xOffset : transform.position.x; //determine the x position
            zPos = useY? (diff.y * ParallaxScale) + yOffset : transform.position.z; //determine the depth position

		    transform.position = new Vector3(xPos, transform.position.y, zPos); //set position
            prevPos = currentCameraPos;
	    }
    }
}
