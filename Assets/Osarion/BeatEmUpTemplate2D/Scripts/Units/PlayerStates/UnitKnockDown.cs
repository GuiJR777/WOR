using UnityEngine;

namespace BeatEmUpTemplate2D {

    // Purpose: Applies a physical knockdown launch, bounce and grounded transition.
    public class UnitKnockDown : State {

        private const float HORIZONTAL_DECELERATION = 1f;
        private const float BOUNCE_VERTICAL_MULTIPLIER = 0.5f;
        private const float MIN_BOUNCE_VELOCITY = 0.05f;
        private const float VERTICAL_ANIMATION_THRESHOLD = 0.05f;

        private readonly string animationNameUp = "KnockDown Up";
        private readonly string animationNameDown = "KnockDown Down";

        public override bool canGrab => false; //unit cannot be grabbed in this state

        private readonly AttackData attackData;
        private readonly float launchHorizontalForce;
        private readonly float launchVerticalForce;
        private readonly float launchDirectionOverride;
        private int bounceNum = 1;
        private float currentHorizontalForce;
        private float launchDirection;
        private float initialVerticalVelocity;
        private bool fallDamageApplied;
        private bool hasHitFloor;
        private bool wasGroundedLastFrame;

        public UnitKnockDown(AttackData attackData, float xForce, float yForce, float launchDirectionOverride = 0f) {
            this.attackData = attackData;
            launchHorizontalForce = Mathf.Max(0f, xForce);
            launchVerticalForce = Mathf.Max(0f, yForce);
            this.launchDirectionOverride = launchDirectionOverride;
        }

        public override void Enter() {
            unit.StopMoving();
            unit.isGrounded = false;
            unit.SetGravityEnabled(true);

            //turn towards direction of the player attack
            if(attackData != null && attackData.inflictor != null && attackData.inflictor.CompareTag("Player")) {
                UnitActions inflictor = attackData.inflictor.GetComponent<UnitActions>();
                if(inflictor != null) {
                    unit.TurnToDir((DIRECTION)((int)inflictor.dir * -1));
                }
            }

            //lose equipped weapon
            if(unit.settings != null && unit.settings.loseWeaponWhenKnockedDown && unit.weapon != null) {
                unit.GetComponentInChildren<WeaponAttachment>()?.LoseCurrentWeapon();
            }

            currentHorizontalForce = launchHorizontalForce;
            launchDirection = ResolveLaunchDirection();

            float speedMultiplier = unit.settings != null ? Mathf.Max(0.01f, unit.settings.knockDownSpeed) : 1f;
            initialVerticalVelocity = launchVerticalForce * speedMultiplier;

            unit.SetCurrentVelocity(new Vector3(launchDirection * currentHorizontalForce, initialVerticalVelocity, 0f));
            wasGroundedLastFrame = unit.isGrounded;
        }

        public override void Update() {
            Vector3 currentVelocity = unit.GetCurrentVelocity();
            if(currentHorizontalForce > 0f) {
                currentHorizontalForce = Mathf.Max(0f, currentHorizontalForce - (HORIZONTAL_DECELERATION * Time.deltaTime));
            }
            unit.SetHorizontalVelocity(launchDirection * currentHorizontalForce, 0f);

            //hit other units when falling down (optional)
            bool goingDown = currentVelocity.y < -VERTICAL_ANIMATION_THRESHOLD;
            bool hurtOtherEnemiesWhenThrown = unit.settings != null
                && unit.settings.hitOtherEnemiesWhenThrown
                && attackData != null
                && attackData.attackType == ATTACKTYPE.GRABTHROW;
            bool hurtOtherEnemiesWhenFalling = unit.settings != null && unit.settings.hitOtherEnemiesWhenFalling;
            if(!hasHitFloor && goingDown && (hurtOtherEnemiesWhenThrown || hurtOtherEnemiesWhenFalling)) {
                unit.CheckForHit(attackData);
            }

            bool justLanded = !wasGroundedLastFrame && unit.isGrounded;
            wasGroundedLastFrame = unit.isGrounded;
            if(justLanded) {
                hasHitFloor = true;

                //if this is a GRABTHROW attack, apply fall damage when this unit hits the floor
                if(!fallDamageApplied && attackData != null && attackData.attackType == ATTACKTYPE.GRABTHROW) {
                    int fallDamage = CombatDamageCalculator.CalculateDamageFromInflictor(
                        attackData,
                        attackData.inflictor,
                        unit.settings,
                        out bool _);

                    if(fallDamage > 0) {
                        unit.GetComponent<HealthSystem>()?.SubstractHealth(fallDamage);
                    }
                    fallDamageApplied = true;
                }

                //keep bouncing until there are no more bounces left
                if(bounceNum > 0) {
                    float bounceVelocity = initialVerticalVelocity * BOUNCE_VERTICAL_MULTIPLIER;
                    if(bounceVelocity > MIN_BOUNCE_VELOCITY) {
                        unit.isGrounded = false;
                        unit.SetVerticalVelocity(bounceVelocity);
                        bounceNum--;
                        unit.CamShake();
                        return;
                    }

                    bounceNum = 0;
                }

                //unit has landed on the floor
                unit.stateMachine.SetState(new UnitKnockDownGrounded());
                return;
            }

            //play up/down animation
            currentVelocity = unit.GetCurrentVelocity();
            if(currentVelocity.y > VERTICAL_ANIMATION_THRESHOLD) {
                unit.animator.Play(animationNameUp);
            } else {
                unit.animator.Play(animationNameDown);
            }
        }

        public override void Exit() {
            unit.SetGravityEnabled(true);
            if(unit.isGrounded) {
                unit.SetHorizontalVelocity(0f, 0f);
                return;
            }

            Vector3 velocity = unit.GetCurrentVelocity();
            velocity.x = 0f;
            velocity.z = 0f;
            unit.SetCurrentVelocity(velocity);
        }

        private float ResolveLaunchDirection() {
            if(Mathf.Abs(launchDirectionOverride) > 0.5f) {
                return Mathf.Sign(launchDirectionOverride);
            }

            if(attackData != null && attackData.inflictor != null) {
                UnitActions inflictorActions = attackData.inflictor.GetComponent<UnitActions>();
                if(inflictorActions != null) {
                    return (int)inflictorActions.dir;
                }
            }

            return -(int)unit.dir;
        }
    }
}
