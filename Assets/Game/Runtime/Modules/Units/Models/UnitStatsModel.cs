// Purpose: Encapsulates all unit stat calculations and runtime modifiers in a pure domain model.
using System.Collections.Generic;
using UnityEngine;
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.Units.Models {

    public enum UnitStatType {
        Constitution = 0,
        Chakra = 10,
        Strength = 20,
        Defense = 30,
        Agility = 40,
        Luck = 50,
    }

    public sealed class UnitStatsModel : IModel {

        private const float DefaultStatMultiplier = 1f;
        private const float HealthPerConstitution = 10f;
        private const float AirMoveSpeedFactor = 0.8f;
        private const float MinStatValue = 0f;
        private const float MinStatMultiplier = 0f;

        private sealed class RuntimeStatModifier {
            public string SourceId;
            public float Additive;
            public float Multiplier = DefaultStatMultiplier;
        }

        private readonly Dictionary<UnitStatType, float> _baseValues = new Dictionary<UnitStatType, float>();
        private readonly Dictionary<UnitStatType, float> _configuredMultipliers = new Dictionary<UnitStatType, float>();
        private readonly Dictionary<UnitStatType, List<RuntimeStatModifier>> _runtimeModifiers =
            new Dictionary<UnitStatType, List<RuntimeStatModifier>>();

        public UnitStatsModel() {
            SetBaseValue(UnitStatType.Constitution, 10f);
            SetBaseValue(UnitStatType.Chakra, 0f);
            SetBaseValue(UnitStatType.Strength, 10f);
            SetBaseValue(UnitStatType.Defense, 0f);
            SetBaseValue(UnitStatType.Agility, 4f);
            SetBaseValue(UnitStatType.Luck, 0f);

            SetConfiguredMultiplier(UnitStatType.Constitution, DefaultStatMultiplier);
            SetConfiguredMultiplier(UnitStatType.Chakra, DefaultStatMultiplier);
            SetConfiguredMultiplier(UnitStatType.Strength, DefaultStatMultiplier);
            SetConfiguredMultiplier(UnitStatType.Defense, DefaultStatMultiplier);
            SetConfiguredMultiplier(UnitStatType.Agility, DefaultStatMultiplier);
            SetConfiguredMultiplier(UnitStatType.Luck, DefaultStatMultiplier);
        }

        public void SetBaseValue(UnitStatType statType, float value) {
            _baseValues[statType] = Mathf.Max(MinStatValue, value);
        }

        public void SetConfiguredMultiplier(UnitStatType statType, float multiplier) {
            _configuredMultipliers[statType] = Mathf.Max(MinStatMultiplier, multiplier);
        }

        public float GetStatValue(UnitStatType statType) {
            float baseValue = _baseValues.TryGetValue(statType, out float value) ? value : 0f;
            float configuredMultiplier = _configuredMultipliers.TryGetValue(statType, out float multiplier)
                ? multiplier
                : DefaultStatMultiplier;

            float additiveBonus = 0f;
            float runtimeMultiplier = 1f;
            if(_runtimeModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                for(int i = 0; i < modifiers.Count; i++) {
                    RuntimeStatModifier modifier = modifiers[i];
                    if(modifier == null) {
                        continue;
                    }

                    additiveBonus += modifier.Additive;
                    runtimeMultiplier *= Mathf.Max(0f, modifier.Multiplier);
                }
            }

            float configuredValue = baseValue * configuredMultiplier;
            float finalValue = (configuredValue + additiveBonus) * runtimeMultiplier;
            return Mathf.Max(0f, finalValue);
        }

        public float GetCriticalChance() {
            return Mathf.Clamp01(GetStatValue(UnitStatType.Luck) / 100f);
        }

        public float GetMoveSpeed() {
            return Mathf.Max(0f, GetStatValue(UnitStatType.Agility));
        }

        public float GetAirMoveSpeed() {
            return GetMoveSpeed() * AirMoveSpeedFactor;
        }

        public int GetMaxHp() {
            return Mathf.Max(1, Mathf.RoundToInt(GetStatValue(UnitStatType.Constitution) * HealthPerConstitution));
        }

        public void SetRuntimeModifier(UnitStatType statType, string sourceId, float additive, float multiplier = 1f) {
            if(string.IsNullOrEmpty(sourceId)) {
                return;
            }

            if(!_runtimeModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                modifiers = new List<RuntimeStatModifier>();
                _runtimeModifiers.Add(statType, modifiers);
            }

            RuntimeStatModifier existingModifier = null;
            for(int i = 0; i < modifiers.Count; i++) {
                RuntimeStatModifier modifier = modifiers[i];
                if(modifier != null && modifier.SourceId == sourceId) {
                    existingModifier = modifier;
                    break;
                }
            }

            if(existingModifier == null) {
                existingModifier = new RuntimeStatModifier();
                existingModifier.SourceId = sourceId;
                modifiers.Add(existingModifier);
            }

            existingModifier.Additive = additive;
            existingModifier.Multiplier = Mathf.Max(0f, multiplier);
        }

        public bool RemoveRuntimeModifier(UnitStatType statType, string sourceId) {
            if(string.IsNullOrEmpty(sourceId)) {
                return false;
            }

            if(!_runtimeModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                return false;
            }

            for(int i = modifiers.Count - 1; i >= 0; i--) {
                RuntimeStatModifier modifier = modifiers[i];
                if(modifier != null && modifier.SourceId == sourceId) {
                    modifiers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public int RemoveAllRuntimeModifiersFromSource(string sourceId) {
            if(string.IsNullOrEmpty(sourceId)) {
                return 0;
            }

            int removed = 0;
            foreach(KeyValuePair<UnitStatType, List<RuntimeStatModifier>> pair in _runtimeModifiers) {
                List<RuntimeStatModifier> modifiers = pair.Value;
                for(int i = modifiers.Count - 1; i >= 0; i--) {
                    RuntimeStatModifier modifier = modifiers[i];
                    if(modifier != null && modifier.SourceId == sourceId) {
                        modifiers.RemoveAt(i);
                        removed++;
                    }
                }
            }
            return removed;
        }

        public void ClearRuntimeModifiers() {
            _runtimeModifiers.Clear();
        }
    }
}

