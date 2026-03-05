using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BeatEmUpTemplate2D {

    // Purpose: Moves projectile instances and resolves sprite-bound collision hits.
    //class for moving projectiles
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour {

        public DIRECTION dir = DIRECTION.RIGHT; //the travel direction
        public float speed = 10; //travel speed
        public float timeToLive = 10f; //destroy after time
        private SpriteRenderer projectileSprite;
        public GameObject hitEffect; //effect to spawn on hit
        public AttackData attackData; //the attackData of this projectile

        [Header("Sprite Collider Offsets")]
        [Help ("* By default the sprite bound is used as the hitbox, use these offsets if you want to make custom changes")]
        public Vector2 spriteBoundSizeOffset; //here you can make changes to the sprite hit collider size
        public Vector2 spriteBoundPositionOffset; //here you can make changes to the sprite hit collider position
        public float depthHitRange = 0.5f;
        public bool showHitbox; //show or hide the hitbox for debug
        private float startTime;
    
        void Start(){
            projectileSprite = GetComponentInChildren<SpriteRenderer>();
            transform.localRotation = Quaternion.Euler(0, dir == DIRECTION.LEFT? 180 : 0, 0); //rotate sprite to face left or right depending on travel direction
            startTime = Time.time;
        }

        void Update() {
            transform.position += Vector3.right * speed * (int)dir * Time.deltaTime;

            //check if we have hit an enemy
            GameObject enemyHit = CheckForHit();
            if(enemyHit){

                //apply damage to enemy
                enemyHit.GetComponent<HealthSystem>()?.SubstractHealth(attackData.damage);

                //play sfx
                if(attackData.sfx.Length > 0) AudioController.PlaySFX(attackData.sfx);

                //get components
                UnitActions ua = enemyHit.GetComponent<UnitActions>();
                UnitSettings us = enemyHit.GetComponent<UnitSettings>();

                //if this attack is a knockdown, go to knockdown state
                if(attackData.knockdown == true && ua?.settings.canBeKnockedDown == true) {
                    float knockdownHorizontalForce = attackData.knockdownLaunchHorizontalForce > 0f
                        ? attackData.knockdownLaunchHorizontalForce
                        : us.knockDownDistance;
                    float knockdownVerticalForce = attackData.knockdownLaunchVerticalForce > 0f
                        ? attackData.knockdownLaunchVerticalForce
                        : us.knockDownHeight;
                    enemyHit.GetComponent<StateMachine>().SetState(new UnitKnockDown(attackData, knockdownHorizontalForce, knockdownVerticalForce));
                }

                //show hit effect
                if(hitEffect) GameObject.Instantiate(hitEffect, transform.position, Quaternion.identity);

                //destroy projectile
                Destroy(gameObject);
            }

            //destroy projectile after time
            if(Time.time - startTime > timeToLive) Destroy(gameObject);
        }

        //check if we hit something
         public GameObject CheckForHit(){

            //create list of enemies
            List<GameObject> hitableObjects = new List<GameObject>(); 
            hitableObjects = GameObject.FindGameObjectsWithTag("Enemy").ToList();
              
            //check if this projectile sprite overlaps with enemy sprites
            hitableObjects = hitableObjects.OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position)).ToList();

            foreach(GameObject target in hitableObjects){
                if(target == null) continue;

                //set bounds
                Bounds bound1 = new Bounds((Vector2)transform.position + spriteBoundPositionOffset, (Vector2)projectileSprite.bounds.size + spriteBoundSizeOffset);
                Bounds bound2 = target.GetComponent<SpriteRenderer>().bounds;
                UnitActions unitActions = target.GetComponent<UnitActions>();
                float targetDepth = unitActions != null ? unitActions.groundPos : target.transform.position.z;
                if(Mathf.Abs(targetDepth - transform.position.z) > depthHitRange) continue;

                //show debug data
                #if UNITY_EDITOR
                if(showHitbox){
                    MathUtilities.DrawRectGizmo(bound1.center, bound1.size, Color.red, Time.deltaTime); 
                    MathUtilities.DrawRectGizmo(bound2.center, bound2.size, Color.red, Time.deltaTime);
                }
                #endif

                //if the two sprite overlap, return the target gameobject as object hit
                if(bound1.Intersects(bound2)) return(target.gameObject);
            }
            return null;
        }

        //Visualize values for Debug in editor
        private void OnDrawGizmos() {
            if(!showHitbox) return;
            if(!projectileSprite){ projectileSprite = GetComponentInChildren<SpriteRenderer>(); return; }
        
            //draw a wireframe cube to visualize the sprite bound
            Gizmos.color = Color.red;
            Bounds bound1 = new Bounds((Vector2)transform.position + spriteBoundPositionOffset, (Vector2)projectileSprite.bounds.size + spriteBoundSizeOffset);
            Gizmos.DrawWireCube(bound1.center, bound1.size);
        }
    }
}
