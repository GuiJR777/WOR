// Purpose: Custom inspector for UnitSettings with grouped foldouts and full combat tuning fields.
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace WOR.Gameplay {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnitSettings))]
    public class UnitSettingsEditor : Editor {

        private static readonly Dictionary<string, bool> FOLDOUTS = new Dictionary<string, bool> {
            { "linked", false },
            { "movement", false },
            { "stats", false },
            { "dash", false },
            { "jump", false },
            { "attack", false },
            { "combo", false },
            { "knockdown", false },
            { "throw", false },
            { "defence", false },
            { "grab", false },
            { "weapon", false },
            { "name", false },
            { "fov", false },
        };

        private const string CAN_BE_KNOCKED_DOWN = "canBeKnockedDown";
        private const string LOAD_RANDOM_NAME_FROM_LIST = "loadRandomNameFromList";
        private const string ENABLE_FOV = "enableFOV";
        private const string CAN_DASH = "canDash";

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawProperty("unitType");
            UNITTYPE unitType = (UNITTYPE)GetProperty("unitType").enumValueIndex;

            DrawSection("stats", "Core Stats", DrawStatsSettings);
            DrawSection("linked", "Linked Components", DrawLinkedComponents);
            DrawSection("movement", "Movement Settings", DrawMovementSettings);

            if(unitType == UNITTYPE.PLAYER) {
                DrawSection("dash", "Dash Settings", DrawDashSettings);
            }

            DrawSection("jump", "Jump Settings", DrawJumpSettings);
            DrawSection("attack", "Attack Data", () => DrawAttackData(unitType));

            if(unitType == UNITTYPE.PLAYER) {
                DrawSection("combo", "Combo Data", DrawComboData);
            }

            DrawSection("knockdown", "Knockdown Settings", DrawKnockdownSettings);
            DrawSection("throw", "Throw Settings", DrawThrowSettings);
            DrawSection("defence", "Defence Settings", () => DrawDefenceSettings(unitType));
            DrawSection("grab", "Grab Settings", () => DrawGrabSettings(unitType));

            if(unitType == UNITTYPE.PLAYER) {
                DrawSection("weapon", "Weapon Settings", DrawWeaponSettings);
            }

            DrawSection("name", "Unit Profile", () => DrawNameSettings(unitType));

            if(unitType == UNITTYPE.ENEMY) {
                DrawSection("fov", "Field Of View Settings", DrawFovSettings);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string key, string label, System.Action drawContent) {
            FOLDOUTS[key] = EditorGUILayout.Foldout(FOLDOUTS[key], label, true, EditorStyles.foldoutHeader);
            if(!FOLDOUTS[key]) {
                return;
            }

            EditorGUI.indentLevel++;
            drawContent?.Invoke();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4f);
        }

        private void DrawLinkedComponents() {
            DrawProperties(
                "shadowPrefab",
                "weaponBone",
                "hitEffect",
                "hitBox",
                "spriteRenderer");

            DrawProperty("useMaterialTint");
            SerializedProperty useMaterialTint = GetProperty("useMaterialTint");
            if(useMaterialTint != null && useMaterialTint.boolValue) {
                DrawProperty("materialTint");
            }
        }

        private void DrawMovementSettings() {
            EditorGUILayout.HelpBox("Move Speed = Agilidade. Move Speed Air = 80% da Agilidade.", MessageType.None);
            DrawProperties(
                "startDirection",
                "depthMoveMultiplier",
                "useAcceleration");

            SerializedProperty useAccelerationProperty = GetProperty("useAcceleration");
            if(useAccelerationProperty != null && useAccelerationProperty.boolValue) {
                DrawProperties("moveAcceleration", "moveDeceleration");
            }
        }

        private void DrawStatsSettings() {
            DrawStatWithMultiplier("constitution", "constitutionMultiplier", "Constituicao");
            DrawStatWithMultiplier("chakra", "chakraMultiplier", "Chakra");
            DrawStatWithMultiplier("strength", "strengthMultiplier", "Forca");
            DrawStatWithMultiplier("defense", "defenseMultiplier", "Defesa");
            DrawStatWithMultiplier("agility", "agilityMultiplier", "Agilidade");
            DrawStatWithMultiplier("luck", "luckMultiplier", "Sorte");
            DrawStatWithMultiplier("burningResistance", "burningResistanceMultiplier", "Resistencia Burning");
            DrawStatWithMultiplier("poisonedResistance", "poisonedResistanceMultiplier", "Resistencia Poisoned");
            DrawStatWithMultiplier("soakedResistance", "soakedResistanceMultiplier", "Resistencia Soaked");
            DrawStatWithMultiplier("bleedingResistance", "bleedingResistanceMultiplier", "Resistencia Bleeding");
            DrawStatWithMultiplier("blindResistance", "blindResistanceMultiplier", "Resistencia Blind");
            DrawStatWithMultiplier("stunnedResistance", "stunnedResistanceMultiplier", "Resistencia Stunned");
            DrawStatWithMultiplier("confusedResistance", "confusedResistanceMultiplier", "Resistencia Confused");
            DrawStatWithMultiplier("electrocutedResistance", "electrocutedResistanceMultiplier", "Resistencia Electrocuted");

            if(serializedObject.isEditingMultipleObjects) {
                return;
            }

            UnitSettings settings = target as UnitSettings;
            if(settings == null) {
                return;
            }

            GUI.enabled = false;
            EditorGUILayout.Space(3f);
            EditorGUILayout.FloatField("Constituicao Final", settings.GetConstitution());
            EditorGUILayout.FloatField("Chakra Final", settings.GetChakra());
            EditorGUILayout.FloatField("Forca Final", settings.GetStrength());
            EditorGUILayout.FloatField("Defesa Final", settings.GetDefense());
            EditorGUILayout.FloatField("Agilidade Final", settings.GetAgility());
            EditorGUILayout.FloatField("Sorte Final", settings.GetLuck());
            EditorGUILayout.FloatField("Resistencia Burning Final", settings.GetBurningResistance());
            EditorGUILayout.FloatField("Resistencia Poisoned Final", settings.GetPoisonedResistance());
            EditorGUILayout.FloatField("Resistencia Soaked Final", settings.GetSoakedResistance());
            EditorGUILayout.FloatField("Resistencia Bleeding Final", settings.GetBleedingResistance());
            EditorGUILayout.FloatField("Resistencia Blind Final", settings.GetBlindResistance());
            EditorGUILayout.FloatField("Resistencia Stunned Final", settings.GetStunnedResistance());
            EditorGUILayout.FloatField("Resistencia Confused Final", settings.GetConfusedResistance());
            EditorGUILayout.FloatField("Resistencia Electrocuted Final", settings.GetElectrocutedResistance());
            EditorGUILayout.IntField("Max HP (Constituicao x 10)", settings.MaxHpFromStats);
            EditorGUILayout.FloatField("Crit Chance (%)", settings.GetCriticalChance() * 100f);
            EditorGUILayout.FloatField("Move Speed", settings.MoveSpeedFromStats);
            EditorGUILayout.FloatField("Move Speed Air", settings.MoveSpeedAirFromStats);
            GUI.enabled = true;
        }

        private void DrawDashSettings() {
            DrawProperty(CAN_DASH);
            SerializedProperty canDashProperty = GetProperty(CAN_DASH);
            if(canDashProperty != null && canDashProperty.boolValue) {
                DrawProperties(
                    "dashSpeed",
                    "dashDuration",
                    "dashCooldown",
                    "dashGhostInterval",
                    "dashInvulnerable");
            }
        }

        private void DrawJumpSettings() {
            DrawProperties("jumpHeight", "jumpSpeed", "jumpGravity");
        }

        private void DrawAttackData(UNITTYPE unitType) {
            if(unitType == UNITTYPE.PLAYER) {
                DrawAttackDataProperty("jumpPunch", "Jump Punch", false);
                DrawAttackDataProperty("jumpKick", "Jump Kick", false);
                DrawAttackDataProperty("grabPunch", "Grab Punch", false);
                DrawAttackDataProperty("grabKick", "Grab Kick", false);
                DrawAttackDataProperty("grabThrow", "Grab Throw", false);
                DrawAttackDataProperty("groundPunch", "Ground Punch", false);
                DrawAttackDataProperty("groundKick", "Ground Kick", false);
                return;
            }

            DrawProperty("enemyPauseBeforeAttack");
            DrawAttackDataArray("enemyAttackList", "Enemy Attack");
        }

        private void DrawComboData() {
            DrawProperties("comboResetTime", "continueComboOnHit");
            DrawAttackComboArray("comboData");
        }

        private void DrawKnockdownSettings() {
            DrawProperty(CAN_BE_KNOCKED_DOWN);
            SerializedProperty canKnockdownProperty = GetProperty(CAN_BE_KNOCKED_DOWN);
            if(canKnockdownProperty == null || !canKnockdownProperty.boolValue) {
                return;
            }

            DrawProperties(
                "knockDownHeight",
                "knockDownDistance",
                "knockDownSpeed",
                "knockDownFloorTime",
                "hitOtherEnemiesWhenFalling");
        }

        private void DrawThrowSettings() {
            DrawProperties("throwHeight", "throwDistance", "hitOtherEnemiesWhenThrown");
        }

        private void DrawDefenceSettings(UNITTYPE unitType) {
            if(unitType == UNITTYPE.ENEMY) {
                DrawProperties("defendChance", "defendDuration");
            } else {
                DrawProperty("canChangeDirWhileDefending");
            }

            DrawProperties(
                "rearDefenseEnabled",
                "parryWindow",
                "parryStunDuration",
                "parryKnockbackForce",
                "parryKnockbackDuration",
                "hitKnockbackForce",
                "hitKnockbackDuration");
        }

        private void DrawGrabSettings(UNITTYPE unitType) {
            if(unitType == UNITTYPE.PLAYER) {
                DrawProperties("grabAnimation", "grabPosition", "grabDuration");
                return;
            }

            DrawProperty("canBeGrabbed");
        }

        private void DrawWeaponSettings() {
            DrawProperties("loseWeaponWhenHit", "loseWeaponWhenKnockedDown");
        }

        private void DrawNameSettings(UNITTYPE unitType) {
            DrawProperties("faction", "unitLevel", "unitRole", "canDetect", "canBeDetected");

            if(unitType == UNITTYPE.PLAYER) {
                DrawProperties("playerId", "unitName", "showNameInAllCaps", "unitPortrait");
                return;
            }

            DrawProperties("unitName", "showNameInAllCaps", "unitPortrait", LOAD_RANDOM_NAME_FROM_LIST);
            SerializedProperty randomNameProperty = GetProperty(LOAD_RANDOM_NAME_FROM_LIST);
            if(randomNameProperty != null && randomNameProperty.boolValue) {
                DrawProperty("unitNamesList");
            }
        }

        private void DrawFovSettings() {
            DrawProperty(ENABLE_FOV);
            SerializedProperty enableFovProperty = GetProperty(ENABLE_FOV);
            if(enableFovProperty != null && enableFovProperty.boolValue) {
                DrawProperties("viewDistance", "viewAngle", "viewPosOffset", "viewHeightOffset", "showFOVCone");
            }

            SerializedProperty targetInSight = GetProperty("targetInSight");
            if(targetInSight != null) {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(targetInSight);
                GUI.enabled = true;
            }
        }

        private void DrawAttackDataArray(string propertyName, string itemPrefix) {
            SerializedProperty attackArray = GetProperty(propertyName);
            if(attackArray == null || !attackArray.isArray) {
                return;
            }

            if(attackArray.arraySize == 0) {
                EditorGUILayout.HelpBox("No attacks configured.", MessageType.Info);
            }

            for(int i = 0; i < attackArray.arraySize; i++) {
                SerializedProperty attackProperty = attackArray.GetArrayElementAtIndex(i);
                DrawAttackDataProperty(attackProperty, $"{itemPrefix} {i + 1}", true);
            }

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Add Attack")) {
                attackArray.InsertArrayElementAtIndex(attackArray.arraySize);
            }
            GUI.enabled = attackArray.arraySize > 0;
            if(GUILayout.Button("Remove Last")) {
                attackArray.DeleteArrayElementAtIndex(attackArray.arraySize - 1);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAttackComboArray(string propertyName) {
            SerializedProperty comboArray = GetProperty(propertyName);
            if(comboArray == null || !comboArray.isArray) {
                return;
            }

            if(comboArray.arraySize == 0) {
                EditorGUILayout.HelpBox("No combos configured.", MessageType.Info);
            }

            for(int i = 0; i < comboArray.arraySize; i++) {
                SerializedProperty comboProperty = comboArray.GetArrayElementAtIndex(i);
                SerializedProperty comboName = comboProperty.FindPropertyRelative("comboName");
                SerializedProperty comboFoldout = comboProperty.FindPropertyRelative("foldout");
                SerializedProperty attackSequence = comboProperty.FindPropertyRelative("attackSequence");

                string comboLabel = comboName != null && !string.IsNullOrEmpty(comboName.stringValue)
                    ? comboName.stringValue
                    : $"Combo {i + 1}";

                if(comboFoldout != null) {
                    comboFoldout.boolValue = EditorGUILayout.Foldout(comboFoldout.boolValue, comboLabel, true);
                    if(!comboFoldout.boolValue) {
                        continue;
                    }
                }

                EditorGUI.indentLevel++;
                if(comboName != null) {
                    EditorGUILayout.PropertyField(comboName, new GUIContent("Combo Name"));
                }

                if(attackSequence != null && attackSequence.isArray) {
                    for(int attackIndex = 0; attackIndex < attackSequence.arraySize; attackIndex++) {
                        SerializedProperty attackProperty = attackSequence.GetArrayElementAtIndex(attackIndex);
                        DrawAttackDataProperty(attackProperty, $"Combo Attack {attackIndex + 1}", true);
                    }

                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("Add Combo Attack")) {
                        attackSequence.InsertArrayElementAtIndex(attackSequence.arraySize);
                    }
                    GUI.enabled = attackSequence.arraySize > 0;
                    if(GUILayout.Button("Remove Last Combo Attack")) {
                        attackSequence.DeleteArrayElementAtIndex(attackSequence.arraySize - 1);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4f);
            }

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Add Combo")) {
                comboArray.InsertArrayElementAtIndex(comboArray.arraySize);
            }
            GUI.enabled = comboArray.arraySize > 0;
            if(GUILayout.Button("Remove Last Combo")) {
                comboArray.DeleteArrayElementAtIndex(comboArray.arraySize - 1);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAttackDataProperty(string propertyName, string label, bool showNameField) {
            SerializedProperty property = GetProperty(propertyName);
            DrawAttackDataProperty(property, label, showNameField);
        }

        private void DrawAttackDataProperty(SerializedProperty property, string label, bool showNameField) {
            if(property == null) {
                return;
            }

            SerializedProperty foldoutProperty = property.FindPropertyRelative("foldout");
            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            SerializedProperty strengthDamageScaleProperty = property.FindPropertyRelative("strengthDamageScale");
            SerializedProperty animationStateProperty = property.FindPropertyRelative("animationState");
            SerializedProperty sfxProperty = property.FindPropertyRelative("sfx");
            SerializedProperty attackTypeProperty = property.FindPropertyRelative("attackType");
            SerializedProperty conditionTypeProperty = property.FindPropertyRelative("conditionType");
            SerializedProperty conditionChargeProperty = property.FindPropertyRelative("conditionCharge");
            SerializedProperty knockdownProperty = property.FindPropertyRelative("knockdown");
            SerializedProperty knockdownLaunchHorizontalForceProperty = property.FindPropertyRelative("knockdownLaunchHorizontalForce");
            SerializedProperty knockdownLaunchVerticalForceProperty = property.FindPropertyRelative("knockdownLaunchVerticalForce");
            SerializedProperty applyKnockbackProperty = property.FindPropertyRelative("applyKnockback");
            SerializedProperty knockbackForceProperty = property.FindPropertyRelative("knockbackForce");
            SerializedProperty knockbackVerticalForceProperty = property.FindPropertyRelative("knockbackVerticalForce");
            SerializedProperty knockbackDurationProperty = property.FindPropertyRelative("knockbackDuration");
            SerializedProperty attackerForwardDistanceProperty = property.FindPropertyRelative("attackerForwardDistance");
            SerializedProperty attackerForwardDurationProperty = property.FindPropertyRelative("attackerForwardDuration");
            SerializedProperty attackerHopVerticalForceProperty = property.FindPropertyRelative("attackerHopVerticalForce");
            SerializedProperty attackerHopOnlyWhenGroundedProperty = property.FindPropertyRelative("attackerHopOnlyWhenGrounded");

            string foldoutLabel = label;
            if(nameProperty != null && !string.IsNullOrEmpty(nameProperty.stringValue)) {
                foldoutLabel = nameProperty.stringValue;
            }

            bool expanded = foldoutProperty == null || foldoutProperty.boolValue;
            expanded = EditorGUILayout.Foldout(expanded, foldoutLabel, true);
            if(foldoutProperty != null) {
                foldoutProperty.boolValue = expanded;
            }
            if(!expanded) {
                return;
            }

            EditorGUI.indentLevel++;
            if(showNameField && nameProperty != null) {
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("Attack Name"));
            }
            if(strengthDamageScaleProperty != null) {
                EditorGUILayout.PropertyField(strengthDamageScaleProperty, new GUIContent("Strength Damage Scale (0-2)"));
            }
            if(animationStateProperty != null) {
                EditorGUILayout.PropertyField(animationStateProperty, new GUIContent("Animation State"));
            }
            if(sfxProperty != null) {
                EditorGUILayout.PropertyField(sfxProperty, new GUIContent("SFX"));
            }
            if(attackTypeProperty != null) {
                EditorGUILayout.PropertyField(attackTypeProperty, new GUIContent("Attack Type"));
            }
            if(conditionTypeProperty != null) {
                EditorGUILayout.PropertyField(conditionTypeProperty, new GUIContent("Condition Type"));
            }
            if(conditionChargeProperty != null) {
                EditorGUILayout.PropertyField(conditionChargeProperty, new GUIContent("Condition Charge (0-1)"));
            }
            if(knockdownProperty != null) {
                EditorGUILayout.PropertyField(knockdownProperty, new GUIContent("Knockdown"));
                if(knockdownProperty.boolValue) {
                    if(knockdownLaunchHorizontalForceProperty != null) {
                        EditorGUILayout.PropertyField(knockdownLaunchHorizontalForceProperty, new GUIContent("Knockdown Launch Horizontal"));
                    }
                    if(knockdownLaunchVerticalForceProperty != null) {
                        EditorGUILayout.PropertyField(knockdownLaunchVerticalForceProperty, new GUIContent("Knockdown Launch Vertical"));
                    }
                }
            }
            if(applyKnockbackProperty != null) {
                EditorGUILayout.PropertyField(applyKnockbackProperty, new GUIContent("Apply Knockback"));
                if(applyKnockbackProperty.boolValue) {
                    if(knockbackForceProperty != null) {
                        EditorGUILayout.PropertyField(knockbackForceProperty, new GUIContent("Knockback Force"));
                    }
                    if(knockbackVerticalForceProperty != null) {
                        EditorGUILayout.PropertyField(knockbackVerticalForceProperty, new GUIContent("Knockback Vertical Force"));
                    }
                    if(knockbackDurationProperty != null) {
                        EditorGUILayout.PropertyField(knockbackDurationProperty, new GUIContent("Knockback Duration"));
                    }
                }
            }
            if(attackerForwardDistanceProperty != null) {
                EditorGUILayout.PropertyField(attackerForwardDistanceProperty, new GUIContent("Attacker Forward Distance"));
                if(attackerForwardDistanceProperty.floatValue > 0f && attackerForwardDurationProperty != null) {
                    EditorGUILayout.PropertyField(attackerForwardDurationProperty, new GUIContent("Attacker Forward Duration"));
                }
            }
            if(attackerHopVerticalForceProperty != null) {
                EditorGUILayout.PropertyField(attackerHopVerticalForceProperty, new GUIContent("Attacker Hop Vertical Force"));
                if(attackerHopVerticalForceProperty.floatValue > 0f && attackerHopOnlyWhenGroundedProperty != null) {
                    EditorGUILayout.PropertyField(attackerHopOnlyWhenGroundedProperty, new GUIContent("Attacker Hop Only When Grounded"));
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2f);
        }

        private void DrawStatWithMultiplier(string valuePropertyName, string multiplierPropertyName, string label) {
            SerializedProperty valueProperty = GetProperty(valuePropertyName);
            SerializedProperty multiplierProperty = GetProperty(multiplierPropertyName);
            if(valueProperty == null || multiplierProperty == null) {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(valueProperty, new GUIContent(label));
            EditorGUILayout.PropertyField(multiplierProperty, new GUIContent("x"), GUILayout.MaxWidth(120f));
            EditorGUILayout.EndHorizontal();
        }

        private SerializedProperty GetProperty(string propertyName) {
            return serializedObject.FindProperty(propertyName);
        }

        private void DrawProperty(string propertyName) {
            SerializedProperty property = GetProperty(propertyName);
            if(property == null) {
                return;
            }
            EditorGUILayout.PropertyField(property);
        }

        private void DrawProperties(params string[] propertyNames) {
            for(int i = 0; i < propertyNames.Length; i++) {
                DrawProperty(propertyNames[i]);
            }
        }
    }
}

