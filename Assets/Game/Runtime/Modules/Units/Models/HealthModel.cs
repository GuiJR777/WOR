// Purpose: Keeps health state and operations decoupled from MonoBehaviour lifecycle.
using UnityEngine;
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.Units.Models {

    public sealed class HealthModel : IModel {

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsInvulnerable { get; set; }
        public bool IsDead => CurrentHp <= 0;
        public float HealthRatio => MaxHp > 0 ? Mathf.Clamp01((float)CurrentHp / MaxHp) : 0f;

        public HealthModel(int maxHp, int currentHp = -1) {
            SetMaxHp(maxHp, currentHp);
        }

        public void SetMaxHp(int maxHp, int currentHp = -1) {
            MaxHp = Mathf.Max(1, maxHp);
            if(currentHp < 0) {
                CurrentHp = MaxHp;
                return;
            }
            CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
        }

        public int ApplyDamage(int damage) {
            if(IsInvulnerable || damage <= 0) {
                return 0;
            }

            int clampedDamage = Mathf.Max(0, damage);
            int hpBefore = CurrentHp;
            CurrentHp = Mathf.Clamp(CurrentHp - clampedDamage, 0, MaxHp);
            return hpBefore - CurrentHp;
        }

        public int Heal(int value) {
            if(value <= 0) {
                return 0;
            }

            int hpBefore = CurrentHp;
            CurrentHp = Mathf.Clamp(CurrentHp + value, 0, MaxHp);
            return CurrentHp - hpBefore;
        }
    }
}

