// Purpose: Coordinates state transitions while delegating gameplay behavior to UnitActions.
using UnityEngine;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(UnitActions))]
    public class StateMachine : MonoBehaviour {

        [SerializeField] private bool showStateInGame;
        [ReadOnlyProperty] public string currentState;

        private TextMesh _stateText;
        private State _state;
        private UnitActions _unit;

        public UnitActions Unit => _unit;

        public GameObject target {
            get {
                return _unit != null ? _unit.target : null;
            }
            set {
                if(_unit != null) {
                    _unit.target = value;
                }
            }
        }

        private void Awake() {
            _unit = GetComponent<UnitActions>();
            if(_unit == null) {
                _unit = gameObject.AddComponent<UnitActions>();
            }
        }

        private void Start() {
            if(_unit == null) {
                return;
            }

            if(_unit.isPlayer) {
                SetState(new PlayerIdle());
                return;
            }

            if(_unit.isEnemy) {
                SetState(new EnemyIdle());
            }
        }

        public void SetState(State nextState) {
            if(nextState == null || _unit == null) {
                return;
            }

            _state?.Exit();

            _state = nextState;
            _state.unit = _unit;
            _state.stateStartTime = Time.time;
            currentState = GetCurrentStateShortName();

            _state.Enter();
        }

        public State GetCurrentState() {
            return _state;
        }

        // Compatibility wrapper kept for legacy callers during migration.
        public GameObject findClosestPlayer() {
            return _unit != null ? _unit.findClosestPlayer() : null;
        }

        // Compatibility wrapper kept for legacy callers during migration.
        public bool targetInSight() {
            return _unit != null && _unit.targetInSight();
        }

        private void Update() {
            _state?.Update();
            UpdateStateText();
        }

        private void LateUpdate() {
            _state?.LateUpdate();
        }

        private void FixedUpdate() {
            _unit?.TickExternalForces();
            _state?.FixedUpdate();
        }

        private void UpdateStateText() {
            if(!showStateInGame) {
                if(_stateText != null) {
                    Destroy(_stateText.gameObject);
                    _stateText = null;
                }
                return;
            }

            if(_stateText == null) {
                GameObject stateTextGo = Instantiate(Resources.Load("StateText"), transform) as GameObject;
                if(stateTextGo == null) {
                    return;
                }

                stateTextGo.name = "StateText";
                stateTextGo.transform.localPosition = new Vector2(0f, -0.2f);
                _stateText = stateTextGo.GetComponent<TextMesh>();
            }

            if(_stateText == null) {
                return;
            }

            _stateText.text = GetCurrentStateShortName();
            float yRotation = _unit != null && _unit.dir == DIRECTION.LEFT ? 180f : 0f;
            _stateText.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        private string GetCurrentStateShortName() {
            if(_state == null) {
                return string.Empty;
            }

            return _state.GetType().Name;
        }
    }
}
