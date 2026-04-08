using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WOR.Gameplay.Modules.Combat.Services;
using WOR.Gameplay.Modules.Units.Services;

namespace WOR.Gameplay {

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
        public bool useInflictorStatsForDamage = true;

        [Header("Sprite Collider Offsets")]
        [Help("* By default the sprite bound is used as the hitbox, use these offsets if you want to make custom changes")]
        public Vector2 spriteBoundSizeOffset; //here you can make changes to the sprite hit collider size
        public Vector2 spriteBoundPositionOffset; //here you can make changes to the sprite hit collider position
        public float depthHitRange = 0.5f;
        public bool showHitbox; //show or hide the hitbox for debug
        private float startTime;

        void Start() {
            projectileSprite = GetComponentInChildren<SpriteRenderer>();
            Vector3 localScale = transform.localScale;
            float absScaleX = Mathf.Abs(localScale.x);
            if(absScaleX <= Mathf.Epsilon) {
                absScaleX = 1f;
            }
            localScale.x = dir == DIRECTION.LEFT ? -absScaleX : absScaleX;
            transform.localScale = localScale;

            Vector3 localEuler = transform.localEulerAngles;
            localEuler.y = 0f;
            transform.localEulerAngles = localEuler;

            if(projectileSprite != null) {
                projectileSprite.flipX = false;
            }
            startTime = Time.time;
        }

        void Update() {
            transform.position += Vector3.right * speed * (int)dir * Time.deltaTime;

            //check if we have hit an enemy
            GameObject targetHit = CheckForHit();
            if(targetHit != null) {
                ResolveHit(targetHit);
            }

            //destroy projectile after time
            if(Time.time - startTime > timeToLive) {
                Destroy(gameObject);
            }
        }

        private void ResolveHit(GameObject targetHit) {
            if(targetHit == null || attackData == null) {
                if(hitEffect) {
                    Instantiate(hitEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
                return;
            }

            if(attackData.inflictor == null) {
                attackData.inflictor = gameObject;
            }

            UnitSettings targetSettings = targetHit.GetComponent<UnitSettings>();
            HealthSystem targetHealth = targetHit.GetComponent<HealthSystem>();

            int finalDamage = ResolveDamage(targetSettings);
            if(targetHealth != null && finalDamage > 0) {
                targetHealth.SubstractHealth(finalDamage);
            }

            ConditionManager targetConditionManager = targetHit.GetComponent<ConditionManager>();
            if(targetConditionManager != null && attackData.conditionType != CONDITIONTYPE.NONE) {
                float conditionCharge = attackData.GetConditionCharge();
                if(conditionCharge > 0f) {
                    targetConditionManager.ApplyConditionCharge(
                        attackData.conditionType,
                        conditionCharge,
                        attackData.inflictor);
                }
            }

            //play sfx
            if(!string.IsNullOrEmpty(attackData.sfx)) {
                AudioController.PlaySFX(attackData.sfx);
            }

            //get components
            UnitActions targetActions = targetHit.GetComponent<UnitActions>();
            StateMachine targetStateMachine = targetHit.GetComponent<StateMachine>();

            if(targetHealth != null && targetHealth.isDead) {
                targetStateMachine?.SetState(new UnitDeath(true));
            } else if(targetActions != null) {
                //if this attack is a knockdown, go to knockdown state
                bool doKnockdown = attackData.knockdown && targetActions.settings != null && targetActions.settings.canBeKnockedDown;
                if(doKnockdown) {
                    float knockdownHorizontalForce = attackData.knockdownLaunchHorizontalForce > 0f
                        ? attackData.knockdownLaunchHorizontalForce
                        : targetActions.settings.knockDownDistance;
                    float knockdownVerticalForce = attackData.knockdownLaunchVerticalForce > 0f
                        ? attackData.knockdownLaunchVerticalForce
                        : targetActions.settings.knockDownHeight;
                    targetStateMachine?.SetState(new UnitKnockDown(attackData, knockdownHorizontalForce, knockdownVerticalForce));
                } else if(targetActions.isGrounded) {
                    targetActions.ApplyRegularHitKnockback(attackData, ResolveAttackDirection(targetHit));
                    targetStateMachine?.SetState(new UnitHit());
                }
            }

            UnitActions.NotifyExternalDealDamage(targetHit, attackData);

            //show hit effect
            if(hitEffect) {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            //destroy projectile
            Destroy(gameObject);
        }

        private int ResolveDamage(UnitSettings targetSettings) {
            if(attackData == null) {
                return 0;
            }

            if(!useInflictorStatsForDamage) {
                return Mathf.Max(0, attackData.damage);
            }

            return CombatDamageCalculator.CalculateDamageFromInflictor(
                attackData,
                attackData.inflictor,
                targetSettings,
                out bool _);
        }

        //check if we hit something
        public GameObject CheckForHit() {
            if(projectileSprite == null) {
                return null;
            }

            List<GameObject> hitableObjects = GetPotentialTargets();
            hitableObjects = hitableObjects
                .Where(obj => obj != null)
                .OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position))
                .ToList();

            foreach(GameObject target in hitableObjects) {
                if(target == null) {
                    continue;
                }

                SpriteRenderer targetSprite = target.GetComponent<SpriteRenderer>();
                if(targetSprite == null) {
                    continue;
                }

                if(!IsWithinDepthRange(target)) {
                    continue;
                }

                //set bounds
                Bounds bound1 = new Bounds((Vector2)transform.position + spriteBoundPositionOffset, (Vector2)projectileSprite.bounds.size + spriteBoundSizeOffset);
                Bounds bound2 = targetSprite.bounds;

                //show debug data
                #if UNITY_EDITOR
                if(showHitbox) {
                    MathUtilities.DrawRectGizmo(bound1.center, bound1.size, Color.red, Time.deltaTime);
                    MathUtilities.DrawRectGizmo(bound2.center, bound2.size, Color.red, Time.deltaTime);
                }
                #endif

                //if the two sprite overlap, return the target gameobject as object hit
                if(bound1.Intersects(bound2)) {
                    return target;
                }
            }
            return null;
        }

        private List<GameObject> GetPotentialTargets() {
            UnitActions inflictorActions = attackData != null && attackData.inflictor != null
                ? attackData.inflictor.GetComponent<UnitActions>()
                : null;

            if(inflictorActions == null) {
                return GameObject.FindGameObjectsWithTag("Enemy").ToList();
            }

            List<GameObject> targets = new List<GameObject>();
            foreach(UnitActions candidate in UnitRegistryService.GetRegisteredUnits()) {
                if(candidate == null || !UnitTargetingService.IsValidCombatTarget(inflictorActions, candidate)) {
                    continue;
                }
                targets.Add(candidate.gameObject);
            }
            return targets;
        }

        private bool IsWithinDepthRange(GameObject target) {
            if(target == null) {
                return false;
            }

            UnitActions targetActions = target.GetComponent<UnitActions>();
            float targetDepth = targetActions != null ? targetActions.groundPos : target.transform.position.z;
            return Mathf.Abs(targetDepth - transform.position.z) <= depthHitRange;
        }

        private DIRECTION ResolveAttackDirection(GameObject targetHit) {
            if(attackData != null && attackData.inflictor != null) {
                UnitActions inflictorActions = attackData.inflictor.GetComponent<UnitActions>();
                if(inflictorActions != null) {
                    return inflictorActions.dir;
                }
            }

            if(targetHit == null) {
                return DIRECTION.RIGHT;
            }

            return targetHit.transform.position.x >= transform.position.x ? DIRECTION.RIGHT : DIRECTION.LEFT;
        }

        //Visualize values for Debug in editor
        private void OnDrawGizmos() {
            if(!showHitbox) {
                return;
            }
            if(!projectileSprite) {
                projectileSprite = GetComponentInChildren<SpriteRenderer>();
                return;
            }

            //draw a wireframe cube to visualize the sprite bound
            Gizmos.color = Color.red;
            Bounds bound1 = new Bounds((Vector2)transform.position + spriteBoundPositionOffset, (Vector2)projectileSprite.bounds.size + spriteBoundSizeOffset);
            Gizmos.DrawWireCube(bound1.center, bound1.size);
        }
    }
}
