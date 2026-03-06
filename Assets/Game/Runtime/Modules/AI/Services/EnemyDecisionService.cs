// Purpose: Encapsulates enemy AI decision rules for attack or reposition behaviors.
using UnityEngine;

namespace WOR.Gameplay.Modules.AI.Services {

    public enum EnemyDecisionType {
        None = 0,
        Attack = 1,
        KeepDistanceClose = 2,
        KeepDistanceFar = 3,
    }

    public readonly struct EnemyDecisionResult {

        public EnemyDecisionType DecisionType { get; }
        public AttackData AttackData { get; }

        public EnemyDecisionResult(EnemyDecisionType decisionType, AttackData attackData = null) {
            DecisionType = decisionType;
            AttackData = attackData;
        }
    }

    public static class EnemyDecisionService {

        public static EnemyDecisionResult BuildDecision(
            bool aiActive,
            bool isIdle,
            int enemyAttackerCount,
            AttackData randomAttack) {
            if(!aiActive || !isIdle) {
                return new EnemyDecisionResult(EnemyDecisionType.None);
            }

            bool shouldAttackWithoutCompetition = enemyAttackerCount == 0 && Random.Range(0, 100) < 75;
            if(shouldAttackWithoutCompetition && randomAttack != null) {
                return new EnemyDecisionResult(EnemyDecisionType.Attack, randomAttack);
            }

            bool shouldAttackWithLowCompetition = enemyAttackerCount <= 2 && Random.Range(0, 100) < 25;
            if(shouldAttackWithLowCompetition && randomAttack != null) {
                return new EnemyDecisionResult(EnemyDecisionType.Attack, randomAttack);
            }

            int randomMove = Random.Range(1, 3);
            return randomMove == 1
                ? new EnemyDecisionResult(EnemyDecisionType.KeepDistanceClose)
                : new EnemyDecisionResult(EnemyDecisionType.KeepDistanceFar);
        }
    }
}
