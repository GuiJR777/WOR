// Purpose: Exposes a global unit registry and cross-module unit queries.
using System.Collections.Generic;
using UnityEngine;
using WOR.Gameplay.Modules.Units.Models;

namespace WOR.Gameplay.Modules.Units.Services {

    public static class UnitRegistryService {

        private static readonly UnitRegistryModel RegistryModel = new UnitRegistryModel();

        public static void Register(UnitActions unit) {
            RegistryModel.Register(unit);
        }

        public static void Unregister(UnitActions unit) {
            RegistryModel.Unregister(unit);
        }

        public static IEnumerable<UnitActions> GetRegisteredUnits() {
            RegistryModel.CleanupDestroyedUnits();
            return RegistryModel.RegisteredUnits;
        }

        public static int CountEnemies(bool includeInactive) {
            HealthSystem[] allHealthSystems = Object.FindObjectsByType<HealthSystem>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            int aliveEnemies = 0;
            for(int i = 0; i < allHealthSystems.Length; i++) {
                HealthSystem healthSystem = allHealthSystems[i];
                if(healthSystem == null || !healthSystem.isEnemy || healthSystem.isDead) {
                    continue;
                }
                aliveEnemies++;
            }

            return aliveEnemies;
        }

        public static int CountEnemyAttackers() {
            int attackers = 0;

            foreach(UnitActions unit in GetRegisteredUnits()) {
                if(unit == null || !unit.isEnemy) {
                    continue;
                }

                StateMachine stateMachine = unit.stateMachine;
                if(stateMachine == null) {
                    continue;
                }

                State currentState = stateMachine.GetCurrentState();
                if(currentState is EnemyAttack || currentState is EnemyMoveToTargetAndAttack) {
                    attackers++;
                }
            }

            return attackers;
        }

        public static List<GameObject> BuildEnemySnapshot(List<GameObject> targetList) {
            if(targetList == null) {
                targetList = new List<GameObject>();
            } else {
                targetList.Clear();
            }

            foreach(UnitActions unit in GetRegisteredUnits()) {
                if(unit == null || !unit.isEnemy) {
                    continue;
                }

                HealthSystem healthSystem = unit.GetComponent<HealthSystem>();
                if(healthSystem != null && healthSystem.isDead) {
                    continue;
                }

                targetList.Add(unit.gameObject);
            }

            return targetList;
        }

        public static void DisableAllEnemyAi() {
            foreach(UnitActions unit in GetRegisteredUnits()) {
                if(unit == null || !unit.isEnemy) {
                    continue;
                }

                EnemyBehaviour enemyBehaviour = unit.GetComponent<EnemyBehaviour>();
                if(enemyBehaviour != null) {
                    enemyBehaviour.AI_Active = false;
                }

                unit.stateMachine?.SetState(new EnemyIdle());
            }
        }
    }
}
