// Purpose: Defines the enemy AI view contract consumed by the AI presenter.
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.AI.Views {

    public interface IEnemyAiView : IView {

        bool IsAiActive { get; }
        float DelayBeforeStart { get; }
        float DecisionInterval { get; }
        float RandomizeAmount { get; }
        StateMachine StateMachine { get; }
        UnitSettings Settings { get; }
        UnitActions UnitActions { get; }

        void SetTargetSpotted(bool spotted);
    }
}
