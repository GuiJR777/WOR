// Purpose: Marks walkable surface zones and links surface-specific footstep SFX.
using UnityEngine;

namespace WOR.Gameplay {

    //class for Surface-Based Footstep Sound Effects
    public class Surface : MonoBehaviour {

       public string footstepSFX = "";

        //check to ensure that the collider is a trigger
        private void OnValidate() {
            Collider col3D = GetComponent<Collider>();
            if(col3D != null && !col3D.isTrigger){
                col3D.isTrigger = true;
                Debug.Log("Set collider of '" + gameObject.name + "' to trigger");
            }

            //check if this gameobject has the surface layer, if not, try to set it to the surface layer
            int surfaceLayer = LayerMask.NameToLayer("Surface");
            if(gameObject.layer != surfaceLayer && surfaceLayer != -1){
                gameObject.layer = surfaceLayer;
                Debug.Log(gameObject.name + " was set to Surface layer");
            }
        }
    }
}

