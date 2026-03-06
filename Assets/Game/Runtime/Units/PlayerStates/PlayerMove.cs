using UnityEngine;

namespace WOR.Gameplay {

    // Purpose: Handles movement state transitions and grounded locomotion.
    public class PlayerMove : State {

        private const float PICKUP_RANGE = 0.4f;

        private string animationName = "Run";
        private int playerId => unit.settings.playerId;

        public override void Update(){

            //dash (dash action button)
            if(InputManager.DashKeyDown(playerId) && unit.IsDashAvailable){ unit.stateMachine.SetState(new PlayerDash()); return; }

            //defend
            if(InputManager.DefendKeyDown(playerId)){ unit.stateMachine.SetState(new UnitDefend()); return; }

            //jump
            if(InputManager.JumpKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerJump()); return; }

            //use weapon
            if(unit.weapon && InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerWeaponAttack()); return; }

            //check for nearby enemy to ground pound
            if(InputManager.PunchKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundPunch()); return; }

            //check for nearby enemy to ground kick
            if(InputManager.KickKeyDown(playerId) && unit.NearbyEnemyDown()){ unit.stateMachine.SetState(new PlayerGroundKick()); return; }

            //punch Key pressed
            if(InputManager.PunchKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.PUNCH)); return; }

            //kick Key pressed
            if(InputManager.KickKeyDown(playerId)){ unit.stateMachine.SetState(new PlayerAttack(ATTACKTYPE.KICK)); return; }

            //grab button: pick up nearby item or throw currently equipped item
            if(InputManager.GrabKeyDown(playerId) && !unit.weapon){
                GameObject pickup = unit.GetClosestPickup(Vector2.one * PICKUP_RANGE);
                if(pickup != null){
                    unit.stateMachine.SetState(new PlayerGrabItem(pickup));
                    return;
                }
            }

            if(InputManager.GrabKeyDown(playerId) && unit.weapon){ unit.stateMachine.SetState(new UnitDropWeapon()); return; }
        }

        public override void FixedUpdate(){

            //get input
            Vector2 inputVector = InputManager.GetInputVector(playerId).normalized;

            //go to idle, if there is no input
            if(inputVector.magnitude == 0) {
                unit.stateMachine.SetState(new PlayerIdle()); 
                return;
            }

            //go to idle if there is a wall in front of us
            Vector2 wallDistanceCheck = unit.GetWallCheckDistance();
            if(unit.WallDetected(inputVector * wallDistanceCheck)){
                unit.stateMachine.SetState(new PlayerIdle()); //go to idle
                return;
            }

            if(unit.TryAutoGrabEnemyFromStep(inputVector, out GameObject enemyToGrab)){
                unit.stateMachine.SetState(new PlayerGrabEnemy(enemyToGrab));
                return;
            }

            //adjust input to move slower in the depth position to create a sense of depth
            inputVector.y *= unit.settings.depthMoveMultiplier; 
                
            //move
            unit.MoveToVector(inputVector, unit.settings.MoveSpeedFromStats);

            //play run anim
            unit.animator.Play(animationName);
        }
    }

    // Purpose: Executes a short directional dash with optional invulnerability and ghost trail.
    public class PlayerDash : State {

        private const string DASH_ANIMATION = "Dash";
        private const float INPUT_DEADZONE = 0.05f;

        private Vector2 _dashVector;
        private float _dashDuration;
        private float _dashSpeed;
        private bool _invulnerabilityEnabled;
        private bool _gravityDisabledForAirDash;
        private HealthSystem _healthSystem;
        private int _playerId => unit.settings.playerId;

        public override bool canGrab => false;

        public override void Enter() {
            if(unit.settings == null || !unit.settings.canDash) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            if(!unit.IsDashAvailable) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            _dashVector = InputManager.GetInputVector(_playerId);
            if(_dashVector.sqrMagnitude <= INPUT_DEADZONE * INPUT_DEADZONE) {
                _dashVector = new Vector2((int)unit.dir, 0f);
            } else {
                _dashVector.Normalize();
            }

            _dashDuration = Mathf.Max(0.05f, unit.settings.dashDuration);
            _dashSpeed = Mathf.Max(unit.settings.MoveSpeedFromStats, unit.settings.dashSpeed);

            unit.MarkDashUsed();
            if(Mathf.Abs(_dashVector.x) > INPUT_DEADZONE) {
                unit.TurnToDir(_dashVector.x > 0f ? DIRECTION.RIGHT : DIRECTION.LEFT);
            }
            unit.animator.Play(DASH_ANIMATION);
            unit.StartGhostTrail(_dashDuration, Mathf.Max(0.01f, unit.settings.dashGhostInterval));

            if(!unit.isGrounded) {
                unit.SetGravityEnabled(false);
                unit.SetVerticalVelocity(0f);
                _gravityDisabledForAirDash = true;
            }

            _healthSystem = unit.GetComponent<HealthSystem>();
            if(_healthSystem != null && unit.settings.dashInvulnerable) {
                _healthSystem.invulnerable = true;
                _invulnerabilityEnabled = true;
            }
        }

        public override void Update() {
            if(Time.time - stateStartTime >= _dashDuration) {
                unit.stateMachine.SetState(new PlayerIdle());
            }
        }

        public override void FixedUpdate() {
            Vector2 wallDistanceCheck = unit.GetWallCheckDistance();
            Vector2 wallCheckDirection = new Vector2(_dashVector.x, _dashVector.y * unit.settings.depthMoveMultiplier);

            if(unit.WallDetected(wallCheckDirection * wallDistanceCheck)) {
                unit.stateMachine.SetState(new PlayerIdle());
                return;
            }

            unit.MoveDashToVector(_dashVector, _dashSpeed);
        }

        public override void Exit() {
            unit.StopGhostTrail();

            if(_gravityDisabledForAirDash) {
                unit.SetGravityEnabled(true);
            }

            unit.StopMoving(true);

            if(_invulnerabilityEnabled && _healthSystem != null) {
                _healthSystem.invulnerable = false;
            }
        }
    }
}

