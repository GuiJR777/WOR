// Purpose: Centralizes player HUD updates for health, chakra, jutsu slots, conditions and visibility rules.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WOR.Gameplay.Modules.Jutsu;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    public class UIPlayerHudController : MonoBehaviour {

        private const float ChakraPerBar = 100f;
        private const int MinBarCost = 1;

        [System.Serializable]
        public sealed class JutsuSlotHudBinding {
            public string slotName = "Jutsu Slot";
            public GameObject slotRoot;
            public Image iconImage;
            public Graphic[] graphicsToTint;
            [Min(MinBarCost)] public int chakraBarCost = MinBarCost;
            public bool hideWhenNoJutsu;
            public Color insufficientChakraColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        }

        [System.Serializable]
        public sealed class ConditionHudBinding {
            public CONDITIONTYPE conditionType = CONDITIONTYPE.NONE;
            public GameObject chargeRoot;
            public Image chargeFillImage;
            public bool showChargeWhenEmpty;
            public GameObject activeIconRoot;
            public Image activeIconImage;
        }

        private sealed class GraphicColorSnapshot {
            public Graphic graphic;
            public Color color;
        }

        [Header("Player Source")]
        public UnitActions playerUnit;
        public bool autoFindPlayer = true;

        [Header("HUD Root")]
        public CanvasGroup hudCanvasGroup;
        public bool autoAddCanvasGroup = true;

        [Header("Visibility Rules")]
        public bool hideWhenPlayerAiControlled = true;
        public bool hideWhenTimeScaleZero = true;
        public bool hideWhenPlayerDead;
        public List<GameObject> hideWhenAnyObjectActive = new List<GameObject>();
        public bool forceVisible;
        public bool forceHidden;

        [Header("HUD Animation")]
        public Animator hudAnimator;
        public string visibleBoolParameter = "Visible";
        public string showTriggerParameter = "Show";
        public string hideTriggerParameter = "Hide";

        [Header("Health")]
        public Image healthFillImage;
        public Text healthText;
        public bool showHealthAsPercent;

        [Header("Chakra")]
        public Image chakraTotalFillImage;
        public List<Image> chakraSegmentFillImages = new List<Image>();
        public Text chakraText;
        public bool showChakraTextAsBars = true;

        [Header("Jutsu Slots")]
        public JutsuSlotHudBinding slot1 = new JutsuSlotHudBinding { slotName = "Slot 1", chakraBarCost = 1 };
        public JutsuSlotHudBinding slot2 = new JutsuSlotHudBinding { slotName = "Slot 2", chakraBarCost = 1 };
        public JutsuSlotHudBinding slot3 = new JutsuSlotHudBinding { slotName = "Slot 3", chakraBarCost = 3 };

        [Header("Conditions")]
        public bool disableLegacyConditionWorldUi = true;
        public bool destroyLegacyConditionWorldUi = true;
        public List<ConditionHudBinding> conditionBindings = new List<ConditionHudBinding>();

        private readonly Dictionary<JutsuSlotHudBinding, List<GraphicColorSnapshot>> _slotOriginalColors =
            new Dictionary<JutsuSlotHudBinding, List<GraphicColorSnapshot>>();

        private HealthSystem _healthSystem;
        private PlayerChakraGauge _chakraGauge;
        private PlayerJutsuController _jutsuController;
        private ConditionManager _conditionManager;
        private StateMachine _stateMachine;

        private bool _hudVisibleState = true;
        private bool _hudVisibleInitialized;

        private bool _hasVisibleBoolParameter;
        private bool _hasShowTriggerParameter;
        private bool _hasHideTriggerParameter;

        private void Awake() {
            EnsureSlotBindings();
            ClampSlotCosts();
            EnsureCanvasGroup();
            CacheAnimatorParameters();
            CacheSlotOriginalColors();
            ResolvePlayerReferences();
        }

        private void Start() {
            RefreshHud(true);
        }

        private void Update() {
            if(playerUnit == null && autoFindPlayer) {
                ResolvePlayerReferences();
            }

            RefreshHud();
        }

        private void OnValidate() {
            EnsureSlotBindings();
            ClampSlotCosts();

            if(Application.isPlaying) {
                CacheAnimatorParameters();
                CacheSlotOriginalColors();
            }
        }

        public void SetForceHidden(bool hidden) {
            forceHidden = hidden;
            if(hidden) {
                forceVisible = false;
            }
            RefreshVisibility(true);
        }

        public void SetForceVisible(bool visible) {
            forceVisible = visible;
            if(visible) {
                forceHidden = false;
            }
            RefreshVisibility(true);
        }

        public void ClearForcedVisibility() {
            forceVisible = false;
            forceHidden = false;
            RefreshVisibility(true);
        }

        public void RefreshNow() {
            RefreshHud(true);
        }

        private void RefreshHud(bool forceFullRefresh = false) {
            UpdateHealthUi();
            UpdateChakraUi();
            UpdateJutsuUi();
            UpdateConditionUi();
            RefreshVisibility(forceFullRefresh);
        }

        private void UpdateHealthUi() {
            if(_healthSystem == null) {
                return;
            }

            if(healthFillImage != null) {
                healthFillImage.fillAmount = _healthSystem.healthPercentage;
            }

            if(healthText != null) {
                if(showHealthAsPercent) {
                    int healthPercent = Mathf.RoundToInt(_healthSystem.healthPercentage * 100f);
                    healthText.text = $"{healthPercent}%";
                } else {
                    healthText.text = $"{_healthSystem.currentHp}/{_healthSystem.maxHp}";
                }
            }
        }

        private void UpdateChakraUi() {
            if(_chakraGauge == null) {
                return;
            }

            float maxChakra = Mathf.Max(1f, _chakraGauge.MaxChakraPercent);
            float currentChakra = Mathf.Clamp(_chakraGauge.currentChakraPercent, 0f, maxChakra);

            if(chakraTotalFillImage != null) {
                chakraTotalFillImage.fillAmount = currentChakra / maxChakra;
            }

            if(chakraSegmentFillImages != null && chakraSegmentFillImages.Count > 0) {
                int maxBars = Mathf.Max(1, _chakraGauge.maxBars);
                for(int i = 0; i < chakraSegmentFillImages.Count; i++) {
                    Image segment = chakraSegmentFillImages[i];
                    if(segment == null) {
                        continue;
                    }

                    bool segmentExists = i < maxBars;
                    segment.gameObject.SetActive(segmentExists);
                    if(!segmentExists) {
                        continue;
                    }

                    float segmentStart = i * ChakraPerBar;
                    float segmentFill = Mathf.Clamp01((currentChakra - segmentStart) / ChakraPerBar);
                    segment.fillAmount = segmentFill;
                }
            }

            if(chakraText != null) {
                if(showChakraTextAsBars) {
                    chakraText.text = $"{_chakraGauge.FilledBars}/{Mathf.Max(1, _chakraGauge.maxBars)}";
                } else {
                    chakraText.text = $"{Mathf.RoundToInt(currentChakra)}/{Mathf.RoundToInt(maxChakra)}";
                }
            }
        }

        private void UpdateJutsuUi() {
            JutsuDefinition slot1Definition = _jutsuController != null ? _jutsuController.slot1Jutsu : null;
            JutsuDefinition slot2Definition = _jutsuController != null ? _jutsuController.slot2Jutsu : null;
            JutsuDefinition slot3Definition = _jutsuController != null ? _jutsuController.slot3Jutsu : null;
            float currentChakra = _chakraGauge != null ? _chakraGauge.currentChakraPercent : 0f;

            UpdateJutsuSlot(slot1, slot1Definition, currentChakra);
            UpdateJutsuSlot(slot2, slot2Definition, currentChakra);
            UpdateJutsuSlot(slot3, slot3Definition, currentChakra);
        }

        private void UpdateJutsuSlot(JutsuSlotHudBinding slotBinding, JutsuDefinition jutsuDefinition, float currentChakra) {
            if(slotBinding == null) {
                return;
            }

            bool hasJutsu = jutsuDefinition != null;
            bool canUse = hasJutsu && currentChakra >= Mathf.Max(MinBarCost, slotBinding.chakraBarCost) * ChakraPerBar;

            if(slotBinding.slotRoot != null && slotBinding.hideWhenNoJutsu) {
                slotBinding.slotRoot.SetActive(hasJutsu);
            }

            if(slotBinding.iconImage != null) {
                slotBinding.iconImage.sprite = hasJutsu ? jutsuDefinition.uiElementImage : null;
                slotBinding.iconImage.enabled = hasJutsu && jutsuDefinition.uiElementImage != null;
            }

            ApplySlotTint(slotBinding, !hasJutsu || canUse);
        }

        private void ApplySlotTint(JutsuSlotHudBinding slotBinding, bool useOriginalColor) {
            List<GraphicColorSnapshot> snapshots = GetOrCreateSlotSnapshots(slotBinding);
            for(int i = 0; i < snapshots.Count; i++) {
                GraphicColorSnapshot snapshot = snapshots[i];
                if(snapshot == null || snapshot.graphic == null) {
                    continue;
                }

                if(useOriginalColor) {
                    snapshot.graphic.color = snapshot.color;
                    continue;
                }

                Color fallback = slotBinding.insufficientChakraColor;
                fallback.a = snapshot.color.a;
                snapshot.graphic.color = fallback;
            }
        }

        private List<GraphicColorSnapshot> GetOrCreateSlotSnapshots(JutsuSlotHudBinding slotBinding) {
            if(slotBinding == null) {
                return new List<GraphicColorSnapshot>();
            }

            if(_slotOriginalColors.TryGetValue(slotBinding, out List<GraphicColorSnapshot> snapshots)) {
                return snapshots;
            }

            snapshots = new List<GraphicColorSnapshot>();
            _slotOriginalColors[slotBinding] = snapshots;

            List<Graphic> graphics = CollectSlotGraphics(slotBinding);
            for(int i = 0; i < graphics.Count; i++) {
                Graphic graphic = graphics[i];
                if(graphic == null) {
                    continue;
                }

                snapshots.Add(new GraphicColorSnapshot {
                    graphic = graphic,
                    color = graphic.color,
                });
            }
            return snapshots;
        }

        private List<Graphic> CollectSlotGraphics(JutsuSlotHudBinding slotBinding) {
            List<Graphic> result = new List<Graphic>();
            if(slotBinding == null) {
                return result;
            }

            if(slotBinding.graphicsToTint != null && slotBinding.graphicsToTint.Length > 0) {
                for(int i = 0; i < slotBinding.graphicsToTint.Length; i++) {
                    Graphic graphic = slotBinding.graphicsToTint[i];
                    if(graphic != null && !result.Contains(graphic)) {
                        result.Add(graphic);
                    }
                }
            }

            if(result.Count == 0 && slotBinding.iconImage != null) {
                result.Add(slotBinding.iconImage);
            }

            return result;
        }

        private void UpdateConditionUi() {
            if(conditionBindings == null || conditionBindings.Count == 0) {
                return;
            }

            if(_conditionManager == null) {
                HideAllConditionUiBindings();
                return;
            }

            for(int i = 0; i < conditionBindings.Count; i++) {
                ConditionHudBinding binding = conditionBindings[i];
                if(binding == null || binding.conditionType == CONDITIONTYPE.NONE) {
                    continue;
                }

                float charge = _conditionManager.GetConditionCharge(binding.conditionType);
                bool isActive = _conditionManager.IsConditionActive(binding.conditionType);
                bool showCharge = binding.showChargeWhenEmpty || isActive || charge > 0f;

                if(binding.chargeFillImage != null) {
                    binding.chargeFillImage.fillAmount = charge;
                }

                if(binding.chargeRoot != null) {
                    binding.chargeRoot.SetActive(showCharge);
                }

                if(binding.activeIconRoot != null) {
                    binding.activeIconRoot.SetActive(isActive);
                }

                if(binding.activeIconImage != null) {
                    binding.activeIconImage.enabled = isActive;
                }
            }
        }

        private void HideAllConditionUiBindings() {
            for(int i = 0; i < conditionBindings.Count; i++) {
                ConditionHudBinding binding = conditionBindings[i];
                if(binding == null) {
                    continue;
                }

                if(binding.chargeFillImage != null) {
                    binding.chargeFillImage.fillAmount = 0f;
                }

                if(binding.chargeRoot != null) {
                    binding.chargeRoot.SetActive(false);
                }

                if(binding.activeIconRoot != null) {
                    binding.activeIconRoot.SetActive(false);
                }

                if(binding.activeIconImage != null) {
                    binding.activeIconImage.enabled = false;
                }
            }
        }

        private void RefreshVisibility(bool forceUpdate = false) {
            bool shouldBeVisible = ComputeShouldHudBeVisible();
            if(!forceUpdate && _hudVisibleInitialized && _hudVisibleState == shouldBeVisible) {
                return;
            }

            _hudVisibleInitialized = true;
            _hudVisibleState = shouldBeVisible;
            ApplyVisibilityState(shouldBeVisible);
        }

        private bool ComputeShouldHudBeVisible() {
            if(forceHidden) {
                return false;
            }

            if(forceVisible) {
                return true;
            }

            if(hideWhenTimeScaleZero && Mathf.Approximately(Time.timeScale, 0f)) {
                return false;
            }

            if(hideWhenPlayerDead && _healthSystem != null && _healthSystem.isDead) {
                return false;
            }

            if(hideWhenPlayerAiControlled && _stateMachine != null) {
                ControledByAI aiState;
                if(_stateMachine.TryGetControledByAiState(out aiState)) {
                    return false;
                }
            }

            if(hideWhenAnyObjectActive != null) {
                for(int i = 0; i < hideWhenAnyObjectActive.Count; i++) {
                    GameObject entry = hideWhenAnyObjectActive[i];
                    if(entry != null && entry.activeInHierarchy) {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ApplyVisibilityState(bool visible) {
            if(hudCanvasGroup != null) {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
                hudCanvasGroup.interactable = visible;
                hudCanvasGroup.blocksRaycasts = visible;
            }

            if(hudAnimator == null) {
                return;
            }

            if(_hasVisibleBoolParameter) {
                hudAnimator.SetBool(visibleBoolParameter, visible);
            }

            if(visible && _hasShowTriggerParameter) {
                hudAnimator.SetTrigger(showTriggerParameter);
            } else if(!visible && _hasHideTriggerParameter) {
                hudAnimator.SetTrigger(hideTriggerParameter);
            }
        }

        private void EnsureCanvasGroup() {
            if(hudCanvasGroup != null) {
                return;
            }

            hudCanvasGroup = GetComponent<CanvasGroup>();
            if(hudCanvasGroup == null && autoAddCanvasGroup) {
                hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void CacheAnimatorParameters() {
            _hasVisibleBoolParameter = HasAnimatorParameter(visibleBoolParameter, AnimatorControllerParameterType.Bool);
            _hasShowTriggerParameter = HasAnimatorParameter(showTriggerParameter, AnimatorControllerParameterType.Trigger);
            _hasHideTriggerParameter = HasAnimatorParameter(hideTriggerParameter, AnimatorControllerParameterType.Trigger);
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType) {
            if(hudAnimator == null || string.IsNullOrWhiteSpace(parameterName)) {
                return false;
            }

            AnimatorControllerParameter[] parameters = hudAnimator.parameters;
            for(int i = 0; i < parameters.Length; i++) {
                AnimatorControllerParameter parameter = parameters[i];
                if(parameter.name == parameterName && parameter.type == parameterType) {
                    return true;
                }
            }
            return false;
        }

        private void CacheSlotOriginalColors() {
            _slotOriginalColors.Clear();
            GetOrCreateSlotSnapshots(slot1);
            GetOrCreateSlotSnapshots(slot2);
            GetOrCreateSlotSnapshots(slot3);
        }

        private void EnsureSlotBindings() {
            if(slot1 == null) {
                slot1 = new JutsuSlotHudBinding { slotName = "Slot 1", chakraBarCost = 1 };
            }

            if(slot2 == null) {
                slot2 = new JutsuSlotHudBinding { slotName = "Slot 2", chakraBarCost = 1 };
            }

            if(slot3 == null) {
                slot3 = new JutsuSlotHudBinding { slotName = "Slot 3", chakraBarCost = 3 };
            }
        }

        private void ClampSlotCosts() {
            if(slot1 != null) {
                slot1.chakraBarCost = Mathf.Max(MinBarCost, slot1.chakraBarCost);
            }

            if(slot2 != null) {
                slot2.chakraBarCost = Mathf.Max(MinBarCost, slot2.chakraBarCost);
            }

            if(slot3 != null) {
                slot3.chakraBarCost = Mathf.Max(MinBarCost, slot3.chakraBarCost);
            }
        }

        private void ResolvePlayerReferences() {
            if((playerUnit == null || playerUnit.gameObject == null) && autoFindPlayer) {
                playerUnit = FindFirstPlayerUnit();
            }

            if(playerUnit == null) {
                _healthSystem = null;
                _chakraGauge = null;
                _jutsuController = null;
                _conditionManager = null;
                _stateMachine = null;
                return;
            }

            _healthSystem = playerUnit.GetComponent<HealthSystem>();
            _chakraGauge = playerUnit.GetComponent<PlayerChakraGauge>();
            _jutsuController = playerUnit.GetComponent<PlayerJutsuController>();
            _conditionManager = playerUnit.GetComponent<ConditionManager>();
            _stateMachine = playerUnit.GetComponent<StateMachine>();

            if(disableLegacyConditionWorldUi && _conditionManager != null) {
                _conditionManager.SetConditionProgressUiEnabled(false, destroyLegacyConditionWorldUi);
            }
        }

        private static UnitActions FindFirstPlayerUnit() {
            UnitActions[] units = FindObjectsByType<UnitActions>(FindObjectsSortMode.None);
            for(int i = 0; i < units.Length; i++) {
                UnitActions candidate = units[i];
                if(candidate != null && candidate.isPlayer) {
                    return candidate;
                }
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            return playerObject != null ? playerObject.GetComponent<UnitActions>() : null;
        }
    }
}
