using System.Collections;
using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Handles reactive bouncing training ball behavior on hit.
    //behaviour for the bouncing ball in the training area
    public class BallBehaviour : MonoBehaviour {

        [SerializeField] private float bounceDuration;
        [SerializeField] private float bounceHeight;
        [SerializeField] private int bounces = 3;
        [SerializeField] private float bounceDistance = 2f;
        [SerializeField] private AnimationCurve bounceCurve;
        [SerializeField] private string bounceSFX = "";
        [SerializeField] private GameObject shadow;

        private Vector3 startPos; //initial position 
        private bool bounceInProgress;

        void OnEnable() {
		    UnitActions.onUnitDealDamage += OnHitObject; //subscribe to event
	    }

	    void OnDisable() {
		    UnitActions.onUnitDealDamage -= OnHitObject; //unsubscribe to event
	    }

        void Start(){
            startPos = transform.position;
            shadow = GameObject.Instantiate(shadow, transform);
            UpdateShadow();
        }

        //when this object was hit, start ball bounce animation
        void OnHitObject(GameObject obj, AttackData attackData){
            if(obj== this.gameObject){
                if(bounceInProgress) StopAllCoroutines(); //stop any bounce in progress
                StartCoroutine(BounceRoutine(attackData)); //start bouncing
            }
        }

        IEnumerator BounceRoutine(AttackData attackData){

            bounceInProgress = true;
            int attackDir = (int)attackData.inflictor.GetComponent<UnitActions>().dir; //check if the attack is coming from left or right

            //start position
            transform.position = new Vector3(transform.position.x, startPos.y, startPos.z);

            //the number of bounces
            for(int bounceCount=1; bounceCount<=bounces; bounceCount++){

                //bounce animation
                float t=0;
                while(t<1f){

                    //calculate new position
                    float xpos = transform.position.x + bounceDistance * Time.deltaTime * attackDir;
                    float ypos = startPos.y + bounceHeight / bounceCount * bounceCurve.Evaluate(t);

                    //don't move in x dir when we've hit a wall (Environment collider)
                    if(EnvironmentCollisionDetected(attackDir)) xpos = transform.position.x;

                    //move ball
                    transform.position = new Vector3(xpos, ypos, startPos.z);
                    UpdateShadow();  

                    //continue
                    t += Time.deltaTime / bounceDuration;
                    yield return 0;
                }

                //play bounce sfx
                if(bounceSFX.Length>0) BeatEmUpTemplate2D.AudioController.PlaySFX(bounceSFX, transform.position);

                //next bounce
                bounceCount ++;
            }
            bounceInProgress = false;
        }

        void UpdateShadow(){
            if(!shadow) return;
            shadow.transform.position = new Vector3(transform.position.x, startPos.y, startPos.z); //set shadow position
        }

        //returns true if we've hit the environment
        bool EnvironmentCollisionDetected(int attackDir){
            float spriteSizeX = GetComponent<SpriteRenderer>().bounds.size.x / 2f;
            Vector3 from = new Vector3(transform.position.x, startPos.y, startPos.z);
            Vector3 direction = Vector3.right * attackDir;
            bool hit = Physics.Raycast(from, direction, spriteSizeX, 1 << LayerMask.NameToLayer("Environment")); //check if we've hit environment layer
            if(!hit){
                Vector2 from2D = new Vector2(transform.position.x, startPos.z);
                Vector2 to2D = from2D + Vector2.right * attackDir * spriteSizeX;
                hit = Physics2D.Linecast(from2D, to2D, 1 << LayerMask.NameToLayer("Environment"));
            }
            Debug.DrawLine(from, from + direction * spriteSizeX, Color.yellow, Time.deltaTime); //show debug line in editor
            return hit;
        }
    }
}
