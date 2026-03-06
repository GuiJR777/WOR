// Purpose: Legacy enemy manager facade backed by modular unit registry services.
using System.Collections.Generic;
using UnityEngine;
using WOR.Gameplay.Modules.Units.Services;

namespace WOR.Gameplay {

    public static class EnemyManager {

        private static readonly List<GameObject> EnemySnapshot = new List<GameObject>();

        // Legacy compatibility field kept for existing callers.
        public static List<GameObject> enemyList => UnitRegistryService.BuildEnemySnapshot(EnemySnapshot);

        public static void RemoveEnemyFromList(GameObject enemy) {
            if(enemy == null) {
                return;
            }

            UnitActions unitActions = enemy.GetComponent<UnitActions>();
            if(unitActions != null) {
                UnitRegistryService.Unregister(unitActions);
            }
        }

        public static void AddEnemyToList(GameObject enemy) {
            if(enemy == null) {
                return;
            }

            UnitActions unitActions = enemy.GetComponent<UnitActions>();
            if(unitActions != null) {
                UnitRegistryService.Register(unitActions);
            }
        }

        public static void DisableAllEnemyAI() {
            UnitRegistryService.DisableAllEnemyAi();
        }

        public static int GetEnemyAttackerCount() {
            return UnitRegistryService.CountEnemyAttackers();
        }

        public static GameObject GetRandomEnemy() {
            List<GameObject> enemies = enemyList;
            if(enemies.Count == 0) {
                return null;
            }

            int randomIndex = Random.Range(0, enemies.Count);
            return enemies[randomIndex];
        }

        public static int GetTotalEnemyCount() {
            return UnitRegistryService.CountEnemies(includeInactive: true);
        }

        public static int GetCurrentEnemyCount() {
            return UnitRegistryService.CountEnemies(includeInactive: false);
        }
    }
}
