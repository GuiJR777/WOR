// Purpose: Enemy AI view component bound to MVP model/presenter flow.
using UnityEngine;
using WOR.Gameplay.Core.Mvp;
using WOR.Gameplay.Modules.AI.Models;
using WOR.Gameplay.Modules.AI.Presenters;
using WOR.Gameplay.Modules.AI.Views;

namespace WOR.Gameplay {

    [RequireComponent(typeof(StateMachine))]
    [RequireComponent(typeof(UnitActions))]
    public class EnemyBehaviour : PresenterBehaviour<IEnemyAiView, EnemyAiModel>, IEnemyAiView {

        public bool AI_Active;
        public float delayBeforeStart = 1f;
        public float decisionInterval = 1f;
        public float randomizeAmount = 1f;
        [ReadOnlyProperty] public bool targetSpotted;

        private StateMachine _stateMachine;
        private UnitSettings _settings;
        private UnitActions _unitActions;

        public bool IsAiActive => AI_Active;
        public float DelayBeforeStart => delayBeforeStart;
        public float DecisionInterval => decisionInterval;
        public float RandomizeAmount => randomizeAmount;
        public StateMachine StateMachine => _stateMachine;
        public UnitSettings Settings => _settings;
        public UnitActions UnitActions => _unitActions;

        protected override void Awake() {
            _stateMachine = GetComponent<StateMachine>();
            _settings = GetComponent<UnitSettings>();
            _unitActions = GetComponent<UnitActions>();
            base.Awake();
        }

        private void Update() {
            if(!IsBound) {
                return;
            }

            EnemyAiRuntimePresenter.Tick(Model, this);
        }

        public void SetTargetSpotted(bool spotted) {
            targetSpotted = spotted;

            if(_unitActions != null) {
                _unitActions.targetSpotted = spotted;
            }
        }

        protected override IEnemyAiView ResolveView() {
            return this;
        }

        protected override EnemyAiModel CreateModel() {
            return new EnemyAiModel();
        }

        protected override void OnBind(EnemyAiModel model, IEnemyAiView view) {
            model.Initialize(Time.time);
            SetTargetSpotted(false);
        }

        protected override void OnUnbind() {
            SetTargetSpotted(false);
        }
    }
}
