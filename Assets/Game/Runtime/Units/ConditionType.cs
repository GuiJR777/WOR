// Purpose: Defines condition identifiers and shared visual palette used by elemental charge systems.
using UnityEngine;

namespace WOR.Gameplay {

    public enum CONDITIONTYPE {
        NONE = 0,
        BURNING = 10,
        POISONED = 20,
        SOAKED = 30,
        BLEEDING = 40,
        BLIND = 50,
        STUNNED = 60,
        CONFUSED = 70,
        ELECTROCUTED = 80,
    }

    public static class ConditionColorPalette {

        private static readonly Color BurningColor = new Color(1f, 0.45f, 0f, 1f);
        private static readonly Color PoisonedColor = new Color(0.25f, 0.85f, 0.25f, 1f);
        private static readonly Color SoakedColor = new Color(0.2f, 0.6f, 1f, 1f);
        private static readonly Color BleedingColor = new Color(0.95f, 0.1f, 0.1f, 1f);
        private static readonly Color BlindColor = new Color(0.45f, 0.25f, 0.7f, 1f);
        private static readonly Color StunnedColor = new Color(1f, 0.9f, 0.2f, 1f);
        private static readonly Color ConfusedColor = new Color(1f, 0.35f, 0.75f, 1f);
        private static readonly Color ElectrocutedColor = new Color(0.25f, 0.95f, 1f, 1f);

        public static Color GetColor(CONDITIONTYPE conditionType) {
            switch(conditionType) {
                case CONDITIONTYPE.BURNING:
                    return BurningColor;
                case CONDITIONTYPE.POISONED:
                    return PoisonedColor;
                case CONDITIONTYPE.SOAKED:
                    return SoakedColor;
                case CONDITIONTYPE.BLEEDING:
                    return BleedingColor;
                case CONDITIONTYPE.BLIND:
                    return BlindColor;
                case CONDITIONTYPE.STUNNED:
                    return StunnedColor;
                case CONDITIONTYPE.CONFUSED:
                    return ConfusedColor;
                case CONDITIONTYPE.ELECTROCUTED:
                    return ElectrocutedColor;
                default:
                    return Color.white;
            }
        }
    }
}

