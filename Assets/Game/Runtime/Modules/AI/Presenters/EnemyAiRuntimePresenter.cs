// Purpose: Drives enemy AI model updates and state transitions using view/service abstractions.
using UnityEngine;
using WOR.Gameplay.Modules.AI.Models;
using WOR.Gameplay.Modules.AI.Services;
using WOR.Gameplay.Modules.AI.Views;

namespace WOR.Gameplay.Modules.AI.Presenters {

    public static class EnemyAiRuntimePresenter {

        public static void Tick(EnemyAiModel model, IEnemyAiView view) {
            if(model == null || view == null) {
                return;
            }

            float timeNow = Time.time;
            if(!model.HasStartDelayElapsed(timeNow, view.DelayBeforeStart)) {
                return;
            }

            UnitActions unitActions = view.UnitActions;
            StateMachine stateMachine = view.StateMachine;
            if(unitActions == null || stateMachine == null) {
                return;
            }

            GameObject closestTarget = unitActions.findClosestPlayer();
            if(stateMachine.target != closestTarget) {
                stateMachine.target = closestTarget;
                model.SetTargetSpotted(false);
                view.SetTargetSpotted(false);
            }

            if(stateMachine.target == null) {
                model.SetTargetSpotted(false);
                view.SetTargetSpotted(false);
                return;
            }

            bool targetInSight = unitActions.targetInSight();
            if(!model.TargetSpotted) {
                if(targetInSight) {
                    model.SetTargetSpotted(true);
                }
            } else if(!targetInSight) {
                model.SetTargetSpotted(false);
            }
            view.SetTargetSpotted(model.TargetSpotted);

            if(!model.TargetSpotted || !model.CanMakeDecision(timeNow, view.DecisionInterval)) {
                return;
            }

            bool isIdle = stateMachine.GetCurrentState() is EnemyIdle;
            if(!view.IsAiActive || !isIdle) {
                return;
            }

            UnitSettings settings = view.Settings;
            AttackData randomAttack = GetRandomAttack(settings);

            EnemyDecisionResult decision = EnemyDecisionService.BuildDecision(
                view.IsAiActive,
                isIdle,
                EnemyManager.GetEnemyAttackerCount(),
                randomAttack);

            float randomDelay = Random.Range(0f, Mathf.Max(0f, view.RandomizeAmount));
            model.MarkDecisionTaken(timeNow + randomDelay);

            ApplyDecision(stateMachine, decision);
        }

        private static AttackData GetRandomAttack(UnitSettings settings) {
            if(settings == null || settings.enemyAttackList.Count == 0) {
                return null;
            }

            int randomIndex = Random.Range(0, settings.enemyAttackList.Count);
            return settings.enemyAttackList[randomIndex];
        }

        private static void ApplyDecision(StateMachine stateMachine, EnemyDecisionResult decision) {
            if(stateMachine == null) {
                return;
            }

            switch(decision.DecisionType) {
                case EnemyDecisionType.Attack:
                    stateMachine.SetState(new EnemyMoveToTargetAndAttack(decision.AttackData));
                    break;
                case EnemyDecisionType.KeepDistanceClose:
                    stateMachine.SetState(new EnemyKeepDistance(2f, 2f, -0.5f, 0.5f));
                    break;
                case EnemyDecisionType.KeepDistanceFar:
                    stateMachine.SetState(new EnemyKeepDistance(4f, 4f, -1f, 1f));
                    break;
            }
        }
    }
}
