// Purpose: Manages condition charge accumulation, activation effects and per-condition floating progress bars.
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace WOR.Gameplay {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitSettings))]
    public class ConditionManager : MonoBehaviour {

        private const float MIN_CHARGE = 0f;
        private const float MAX_CHARGE = 1f;
        private const float MIN_SECONDS = 0f;
        private const float DEFAULT_CHARGE_DECAY = 0.08f;
        private const float DEFAULT_RESISTANCE_DECAY_BONUS = 0.3f;
        private const float DEFAULT_BURNING_DAMAGE_PER_SECOND = 2.5f;
        private const float DEFAULT_ELECTROCUTED_DAMAGE_PER_SECOND = 2f;
        private const float DEFAULT_BURNING_DAMAGE_INTERVAL = 3f;
        private const float DEFAULT_ELECTROCUTED_DAMAGE_INTERVAL = 3f;
        private const float DEFAULT_POISONED_DAMAGE_INTERVAL = 1f;
        private const int DEFAULT_POISONED_DAMAGE_PER_TICK = 1;
        private const float DEFAULT_ELECTROCUTED_SPREAD_RADIUS = 1.1f;
        private const float DEFAULT_ELECTROCUTED_SPREAD_CHARGE = 0.12f;
        private const float DEFAULT_ELECTROCUTED_SPREAD_INTERVAL = 0.35f;
        private const float DEFAULT_SOAKED_AGILITY_MULTIPLIER = 0.45f;
        private const float DEFAULT_BLEEDING_STRENGTH_MULTIPLIER = 0.7f;
        private const float DEFAULT_BLEEDING_DEFENSE_MULTIPLIER = 0.7f;
        private const float DEFAULT_ELECTROCUTED_AGILITY_MULTIPLIER = 0.7f;
        private const float DEFAULT_STUN_REFRESH_DURATION = 0.2f;
        private const float DEFAULT_POISONED_MOVING_THRESHOLD = 0.1f;
        private const float DEFAULT_UI_SCALE = 0.01f;
        private const float DEFAULT_UI_PIXELS = 64f;
        private const int DEFAULT_UI_SORTING_ORDER = 260;
        private const float DEFAULT_UI_Y_FROM_HEALTH_BAR = -0.16f;
        private const float UI_SLOT_SIZE_FACTOR = 0.34f;
        private const float UI_SLOT_SPACING_FACTOR = 0.08f;

        private const string SOAKED_SOURCE_ID = "condition_soaked";
        private const string BLEEDING_STRENGTH_SOURCE_ID = "condition_bleeding_strength";
        private const string BLEEDING_DEFENSE_SOURCE_ID = "condition_bleeding_defense";
        private const string ELECTROCUTED_SOURCE_ID = "condition_electrocuted";

        private static readonly CONDITIONTYPE[] TrackedConditions = {
            CONDITIONTYPE.BURNING,
            CONDITIONTYPE.POISONED,
            CONDITIONTYPE.SOAKED,
            CONDITIONTYPE.BLEEDING,
            CONDITIONTYPE.BLIND,
            CONDITIONTYPE.STUNNED,
            CONDITIONTYPE.CONFUSED,
            CONDITIONTYPE.ELECTROCUTED,
        };

        [Header("Charge Settings")]
        [Min(MIN_SECONDS)] public float baseChargeDecayPerSecond = DEFAULT_CHARGE_DECAY;
        [Min(MIN_SECONDS)] public float resistanceDecayBonusPerSecond = DEFAULT_RESISTANCE_DECAY_BONUS;

        [Header("Damage")]
        [Min(0f)] public float burningDamagePerSecond = DEFAULT_BURNING_DAMAGE_PER_SECOND;
        [Min(0f)] public float electrocutedDamagePerSecond = DEFAULT_ELECTROCUTED_DAMAGE_PER_SECOND;
        [Min(0.05f)] public float burningDamageInterval = DEFAULT_BURNING_DAMAGE_INTERVAL;
        [Min(0.05f)] public float electrocutedDamageInterval = DEFAULT_ELECTROCUTED_DAMAGE_INTERVAL;
        [Min(0.05f)] public float poisonedDamageInterval = DEFAULT_POISONED_DAMAGE_INTERVAL;
        [Min(1)] public int poisonedDamagePerTick = DEFAULT_POISONED_DAMAGE_PER_TICK;
        [Min(0f)] public float poisonedMovingSpeedThreshold = DEFAULT_POISONED_MOVING_THRESHOLD;

        [Header("Electrocuted Spread")]
        [Min(0f)] public float electrocutedSpreadRadius = DEFAULT_ELECTROCUTED_SPREAD_RADIUS;
        [Range(MIN_CHARGE, MAX_CHARGE)] public float electrocutedSpreadCharge = DEFAULT_ELECTROCUTED_SPREAD_CHARGE;
        [Min(MIN_SECONDS)] public float electrocutedSpreadInterval = DEFAULT_ELECTROCUTED_SPREAD_INTERVAL;

        [Header("Debuff Multipliers")]
        [Range(0f, 1f)] public float soakedAgilityMultiplier = DEFAULT_SOAKED_AGILITY_MULTIPLIER;
        [Range(0f, 1f)] public float bleedingStrengthMultiplier = DEFAULT_BLEEDING_STRENGTH_MULTIPLIER;
        [Range(0f, 1f)] public float bleedingDefenseMultiplier = DEFAULT_BLEEDING_DEFENSE_MULTIPLIER;
        [Range(0f, 1f)] public float electrocutedAgilityMultiplier = DEFAULT_ELECTROCUTED_AGILITY_MULTIPLIER;
        [Min(MIN_SECONDS)] public float stunnedRefreshDuration = DEFAULT_STUN_REFRESH_DURATION;

        [Header("Condition UI")]
        public bool showConditionProgress = true;
        public Vector3 defaultUiOffset = new Vector3(0f, 1f, 0f);
        public float uiYOffsetFromHealthBar = DEFAULT_UI_Y_FROM_HEALTH_BAR;
        [Min(0.001f)] public float uiScale = DEFAULT_UI_SCALE;
        [Min(16f)] public float uiPixelSize = DEFAULT_UI_PIXELS;
        public int uiSortingOrder = DEFAULT_UI_SORTING_ORDER;

        private sealed class ConditionState {
            public float Charge;
            public bool IsActive;
            public float DamageTickTimer;
            public float SpreadTimer;
        }

        private sealed class ConditionUiEntry {
            public RectTransform Root;
            public Image Background;
            public Image Fill;
            public Image Icon;
        }

        private readonly Dictionary<CONDITIONTYPE, ConditionState> _states =
            new Dictionary<CONDITIONTYPE, ConditionState>();

        private readonly Dictionary<CONDITIONTYPE, ConditionUiEntry> _uiEntries =
            new Dictionary<CONDITIONTYPE, ConditionUiEntry>();

        private UnitSettings _unitSettings;
        private HealthSystem _healthSystem;
        private StateMachine _stateMachine;
        private UnitActions _unitActions;

        private bool _blindPreviousCanDetect;
        private bool _confusedApplied;
        private UNITFACTION _confusedPreviousFaction;
        private bool _onApplicationQuit;

        private GameObject _uiRoot;
        private RectTransform _uiRootTransform;
        private Camera _cachedMainCamera;

        private static Sprite _circleSprite;

        private void Awake() {
            _unitSettings = GetComponent<UnitSettings>();
            _healthSystem = GetComponent<HealthSystem>();
            _stateMachine = GetComponent<StateMachine>();
            _unitActions = GetComponent<UnitActions>();
            InitializeStates();
            ClampConfiguration();
        }

        private void Start() {
            if(showConditionProgress) {
                EnsureUi();
            }
        }

        private void OnDisable() {
            ClearRuntimeEffects();
        }

        private void OnDestroy() {
            DestroyUiRoot();
        }

        private void OnApplicationQuit() {
            _onApplicationQuit = true;
        }

        private void OnValidate() {
            ClampConfiguration();
        }

        private void Update() {
            TickChargeAndEffects(Time.deltaTime);
            RefreshUiView();
        }

        private void LateUpdate() {
            FollowTargetWithUi();
        }

        public bool IsConditionActive(CONDITIONTYPE conditionType) {
            if(conditionType == CONDITIONTYPE.NONE) {
                return false;
            }

            ConditionState state = GetState(conditionType);
            return state != null && state.IsActive;
        }

        public float GetConditionCharge(CONDITIONTYPE conditionType) {
            if(conditionType == CONDITIONTYPE.NONE) {
                return 0f;
            }

            ConditionState state = GetState(conditionType);
            return state != null ? Mathf.Clamp01(state.Charge) : 0f;
        }

        public void ApplyConditionCharge(CONDITIONTYPE conditionType, float chargeAmount, GameObject source = null) {
            if(conditionType == CONDITIONTYPE.NONE || chargeAmount <= 0f) {
                return;
            }

            ConditionState state = GetState(conditionType);
            if(state == null || state.IsActive) {
                return;
            }

            float resistance = GetResistanceNormalized(conditionType);
            float adjustedCharge = Mathf.Max(0f, chargeAmount) * (1f - resistance);
            if(adjustedCharge <= 0f) {
                return;
            }

            state.Charge = Mathf.Clamp01(state.Charge + adjustedCharge);
            if(state.Charge >= MAX_CHARGE) {
                ActivateCondition(conditionType, state);
            }
        }

        public void ApplyConditionChargePerSecond(CONDITIONTYPE conditionType, float chargePerSecond, GameObject source = null) {
            if(chargePerSecond <= 0f) {
                return;
            }

            ApplyConditionCharge(conditionType, chargePerSecond * Time.deltaTime, source);
        }

        public void ClearAllConditions() {
            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                ConditionState state = GetState(conditionType);
                if(state == null) {
                    continue;
                }

                if(state.IsActive) {
                    DeactivateCondition(conditionType, state);
                }

                state.Charge = 0f;
                state.DamageTickTimer = 0f;
                state.SpreadTimer = 0f;
            }
        }

        public void SetConditionProgressUiEnabled(bool enabled, bool destroyExistingUi = false) {
            showConditionProgress = enabled;

            if(enabled) {
                EnsureUi();
                RefreshUiView();
                return;
            }

            HideUi();
            if(destroyExistingUi) {
                DestroyUiRoot();
            }
        }

        private void InitializeStates() {
            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                if(_states.ContainsKey(conditionType)) {
                    continue;
                }

                _states.Add(conditionType, new ConditionState());
            }
        }

        private void ClampConfiguration() {
            baseChargeDecayPerSecond = Mathf.Max(0f, baseChargeDecayPerSecond);
            resistanceDecayBonusPerSecond = Mathf.Max(0f, resistanceDecayBonusPerSecond);
            burningDamagePerSecond = Mathf.Max(0f, burningDamagePerSecond);
            electrocutedDamagePerSecond = Mathf.Max(0f, electrocutedDamagePerSecond);
            burningDamageInterval = Mathf.Max(0.05f, burningDamageInterval);
            electrocutedDamageInterval = Mathf.Max(0.05f, electrocutedDamageInterval);
            poisonedDamageInterval = Mathf.Max(0.05f, poisonedDamageInterval);
            poisonedDamagePerTick = Mathf.Max(1, poisonedDamagePerTick);
            poisonedMovingSpeedThreshold = Mathf.Max(0f, poisonedMovingSpeedThreshold);
            electrocutedSpreadRadius = Mathf.Max(0f, electrocutedSpreadRadius);
            electrocutedSpreadCharge = Mathf.Clamp01(electrocutedSpreadCharge);
            electrocutedSpreadInterval = Mathf.Max(0.01f, electrocutedSpreadInterval);
            soakedAgilityMultiplier = Mathf.Clamp01(soakedAgilityMultiplier);
            bleedingStrengthMultiplier = Mathf.Clamp01(bleedingStrengthMultiplier);
            bleedingDefenseMultiplier = Mathf.Clamp01(bleedingDefenseMultiplier);
            electrocutedAgilityMultiplier = Mathf.Clamp01(electrocutedAgilityMultiplier);
            stunnedRefreshDuration = Mathf.Max(0.05f, stunnedRefreshDuration);
            uiScale = Mathf.Max(0.001f, uiScale);
            uiPixelSize = Mathf.Max(16f, uiPixelSize);
        }

        private ConditionState GetState(CONDITIONTYPE conditionType) {
            if(!_states.TryGetValue(conditionType, out ConditionState state)) {
                state = new ConditionState();
                _states[conditionType] = state;
            }
            return state;
        }

        private float GetResistanceNormalized(CONDITIONTYPE conditionType) {
            if(_unitSettings == null) {
                return 0f;
            }

            float rawResistance = Mathf.Max(0f, _unitSettings.GetConditionResistance(conditionType));
            if(rawResistance > 1f) {
                rawResistance *= 0.01f;
            }
            return Mathf.Clamp01(rawResistance);
        }

        private void TickChargeAndEffects(float deltaTime) {
            if(deltaTime <= 0f) {
                return;
            }

            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                ConditionState state = GetState(conditionType);
                if(state == null || state.Charge <= 0f && !state.IsActive) {
                    continue;
                }

                float resistance = GetResistanceNormalized(conditionType);
                float decayRate = baseChargeDecayPerSecond + resistance * resistanceDecayBonusPerSecond;
                state.Charge = Mathf.Max(0f, state.Charge - decayRate * deltaTime);

                if(state.IsActive) {
                    TickConditionEffect(conditionType, state, deltaTime);
                    if(state.Charge <= 0f) {
                        DeactivateCondition(conditionType, state);
                    }
                } else if(state.Charge <= 0f) {
                    state.DamageTickTimer = 0f;
                    state.SpreadTimer = 0f;
                }
            }
        }

        private void ActivateCondition(CONDITIONTYPE conditionType, ConditionState state) {
            if(state == null || state.IsActive) {
                return;
            }

            state.IsActive = true;
            state.Charge = MAX_CHARGE;
            state.DamageTickTimer = 0f;
            state.SpreadTimer = 0f;

            if(_unitSettings == null) {
                return;
            }

            switch(conditionType) {
                case CONDITIONTYPE.SOAKED:
                    _unitSettings.SetStatModifier(UNITSTATTYPE.AGILITY, SOAKED_SOURCE_ID, 0f, soakedAgilityMultiplier);
                    break;
                case CONDITIONTYPE.BLEEDING:
                    _unitSettings.SetStatModifier(UNITSTATTYPE.STRENGTH, BLEEDING_STRENGTH_SOURCE_ID, 0f, bleedingStrengthMultiplier);
                    _unitSettings.SetStatModifier(UNITSTATTYPE.DEFENSE, BLEEDING_DEFENSE_SOURCE_ID, 0f, bleedingDefenseMultiplier);
                    break;
                case CONDITIONTYPE.BLIND:
                    _blindPreviousCanDetect = _unitSettings.canDetect;
                    _unitSettings.canDetect = false;
                    break;
                case CONDITIONTYPE.STUNNED:
                    ForceStunnedState();
                    break;
                case CONDITIONTYPE.CONFUSED:
                    _confusedPreviousFaction = _unitSettings.faction;
                    _unitSettings.faction = UNITFACTION.CONFUSED;
                    _confusedApplied = true;
                    break;
                case CONDITIONTYPE.ELECTROCUTED:
                    _unitSettings.SetStatModifier(UNITSTATTYPE.AGILITY, ELECTROCUTED_SOURCE_ID, 0f, electrocutedAgilityMultiplier);
                    break;
            }
        }

        private void DeactivateCondition(CONDITIONTYPE conditionType, ConditionState state) {
            if(state == null || !state.IsActive) {
                return;
            }

            state.IsActive = false;
            state.DamageTickTimer = 0f;
            state.SpreadTimer = 0f;

            if(_unitSettings == null) {
                return;
            }

            switch(conditionType) {
                case CONDITIONTYPE.SOAKED:
                    _unitSettings.RemoveStatModifier(UNITSTATTYPE.AGILITY, SOAKED_SOURCE_ID);
                    break;
                case CONDITIONTYPE.BLEEDING:
                    _unitSettings.RemoveStatModifier(UNITSTATTYPE.STRENGTH, BLEEDING_STRENGTH_SOURCE_ID);
                    _unitSettings.RemoveStatModifier(UNITSTATTYPE.DEFENSE, BLEEDING_DEFENSE_SOURCE_ID);
                    break;
                case CONDITIONTYPE.BLIND:
                    _unitSettings.canDetect = _blindPreviousCanDetect;
                    break;
                case CONDITIONTYPE.STUNNED:
                    ReleaseStunnedState();
                    break;
                case CONDITIONTYPE.CONFUSED:
                    if(_confusedApplied) {
                        _unitSettings.faction = _confusedPreviousFaction;
                    }
                    _confusedApplied = false;
                    break;
                case CONDITIONTYPE.ELECTROCUTED:
                    _unitSettings.RemoveStatModifier(UNITSTATTYPE.AGILITY, ELECTROCUTED_SOURCE_ID);
                    break;
            }
        }

        private void TickConditionEffect(CONDITIONTYPE conditionType, ConditionState state, float deltaTime) {
            switch(conditionType) {
                case CONDITIONTYPE.BURNING:
                    TickPeriodicDamage(
                        state,
                        Mathf.RoundToInt(Mathf.Max(0f, burningDamagePerSecond)),
                        burningDamageInterval,
                        deltaTime,
                        conditionType);
                    break;
                case CONDITIONTYPE.POISONED:
                    TickPoisonedDamageOnlyWhenMoving(state, deltaTime, conditionType);
                    break;
                case CONDITIONTYPE.STUNNED:
                    ForceStunnedState();
                    break;
                case CONDITIONTYPE.ELECTROCUTED:
                    TickPeriodicDamage(
                        state,
                        Mathf.RoundToInt(Mathf.Max(0f, electrocutedDamagePerSecond)),
                        electrocutedDamageInterval,
                        deltaTime,
                        conditionType);
                    TickElectrocutedSpread(state, deltaTime);
                    break;
            }
        }

        private void TickPoisonedDamageOnlyWhenMoving(ConditionState state, float deltaTime, CONDITIONTYPE conditionType) {
            if(state == null || _unitActions == null) {
                return;
            }

            Vector3 velocity = _unitActions.GetCurrentVelocity();
            Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);
            if(horizontalVelocity.magnitude < poisonedMovingSpeedThreshold) {
                return;
            }

            TickPeriodicDamage(state, poisonedDamagePerTick, poisonedDamageInterval, deltaTime, conditionType);
        }

        private void TickPeriodicDamage(
            ConditionState state,
            int damagePerTick,
            float tickInterval,
            float deltaTime,
            CONDITIONTYPE conditionType) {
            if(state == null || damagePerTick <= 0 || tickInterval <= 0f || _healthSystem == null || _healthSystem.isDead) {
                return;
            }

            state.DamageTickTimer += deltaTime;
            if(state.DamageTickTimer < tickInterval) {
                return;
            }

            int ticksToApply = Mathf.FloorToInt(state.DamageTickTimer / tickInterval);
            state.DamageTickTimer -= ticksToApply * tickInterval;
            ApplyConditionDamage(damagePerTick * ticksToApply, conditionType);
        }

        private void ApplyConditionDamage(int damage, CONDITIONTYPE conditionType) {
            if(damage <= 0 || _healthSystem == null || _healthSystem.isDead) {
                return;
            }

            Color conditionColor = ConditionColorPalette.GetColor(conditionType);
            _healthSystem.SubstractHealth(damage, true, conditionColor);

            if(!_healthSystem.isDead || _stateMachine == null) {
                return;
            }

            if(!(_stateMachine.GetCurrentState() is UnitDeath)) {
                _stateMachine.SetState(new UnitDeath(true));
            }
        }

        private void TickElectrocutedSpread(ConditionState state, float deltaTime) {
            if(state == null || electrocutedSpreadRadius <= 0f || electrocutedSpreadCharge <= 0f) {
                return;
            }

            state.SpreadTimer += deltaTime;
            if(state.SpreadTimer < electrocutedSpreadInterval) {
                return;
            }

            state.SpreadTimer = 0f;

            Collider[] overlaps = Physics.OverlapSphere(
                transform.position,
                electrocutedSpreadRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for(int i = 0; i < overlaps.Length; i++) {
                Collider overlap = overlaps[i];
                if(overlap == null || overlap.gameObject == gameObject) {
                    continue;
                }

                ConditionManager otherManager = overlap.GetComponent<ConditionManager>();
                if(otherManager == null) {
                    continue;
                }

                otherManager.ApplyConditionCharge(CONDITIONTYPE.ELECTROCUTED, electrocutedSpreadCharge, gameObject);
            }
        }

        private void ForceStunnedState() {
            if(_stateMachine == null || _healthSystem == null || _healthSystem.isDead) {
                return;
            }

            if(!(_stateMachine.GetCurrentState() is UnitStunned)) {
                _stateMachine.SetState(new UnitStunned(stunnedRefreshDuration));
            }
        }

        private void ReleaseStunnedState() {
            if(_stateMachine == null || _healthSystem == null || _healthSystem.isDead) {
                return;
            }

            if(!(_stateMachine.GetCurrentState() is UnitStunned)) {
                return;
            }

            if(_unitActions != null && _unitActions.isPlayer) {
                _stateMachine.SetState(new PlayerIdle());
            } else {
                _stateMachine.SetState(new EnemyIdle());
            }
        }

        private void ClearRuntimeEffects() {
            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                ConditionState state = GetState(conditionType);
                if(state != null && state.IsActive) {
                    DeactivateCondition(conditionType, state);
                }
            }
        }

        private void EnsureUi() {
            if(!showConditionProgress || _uiRoot != null) {
                return;
            }

            _uiRoot = new GameObject($"{name}_ConditionProgressUI");
            _uiRootTransform = _uiRoot.AddComponent<RectTransform>();

            Canvas canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = uiSortingOrder;

            GraphicRaycaster raycaster = _uiRoot.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            _uiRootTransform.sizeDelta = new Vector2(uiPixelSize * 2f, uiPixelSize);
            _uiRootTransform.localScale = Vector3.one * uiScale;

            CreateConditionUiEntries();
            _uiRoot.SetActive(false);
        }

        private void CreateConditionUiEntries() {
            _uiEntries.Clear();

            float slotSize = uiPixelSize * UI_SLOT_SIZE_FACTOR;
            float spacing = slotSize + uiPixelSize * UI_SLOT_SPACING_FACTOR;
            float totalWidth = (TrackedConditions.Length - 1) * spacing;
            float startX = -0.5f * totalWidth;

            Sprite circleSprite = GetCircleSprite();

            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                float xPosition = startX + i * spacing;

                GameObject slotGo = new GameObject($"{conditionType}_Slot");
                slotGo.transform.SetParent(_uiRootTransform, false);
                RectTransform slotRect = slotGo.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = Vector2.one * slotSize;
                slotRect.anchoredPosition = new Vector2(xPosition, 0f);

                ConditionUiEntry entry = new ConditionUiEntry {
                    Root = slotRect,
                    Background = CreateUiImage(slotRect, "Background", circleSprite, Color.black * 0.5f, slotSize),
                    Fill = CreateUiImage(slotRect, "Progress", circleSprite, Color.white, slotSize * 0.92f),
                    Icon = CreateUiImage(slotRect, "Icon", circleSprite, Color.white, slotSize * 0.45f),
                };

                entry.Fill.type = Image.Type.Filled;
                entry.Fill.fillMethod = Image.FillMethod.Radial360;
                entry.Fill.fillOrigin = (int)Image.Origin360.Top;
                entry.Fill.fillClockwise = true;
                entry.Fill.fillAmount = 0f;
                entry.Icon.enabled = false;
                slotGo.SetActive(false);

                _uiEntries.Add(conditionType, entry);
            }
        }

        private static Image CreateUiImage(Transform parent, string objectName, Sprite sprite, Color color, float size) {
            GameObject imageGo = new GameObject(objectName);
            imageGo.transform.SetParent(parent, false);

            RectTransform rect = imageGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size;
            rect.anchoredPosition = Vector2.zero;

            Image image = imageGo.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite GetCircleSprite() {
            if(_circleSprite != null) {
                return _circleSprite;
            }

            const int textureSize = 64;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
            float radius = textureSize * 0.5f - 1f;

            for(int y = 0; y < textureSize; y++) {
                for(int x = 0; x < textureSize; x++) {
                    Vector2 point = new Vector2(x, y);
                    float distance = Vector2.Distance(point, center);
                    float alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            _circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                textureSize);
            _circleSprite.name = "RuntimeConditionCircle";
            return _circleSprite;
        }

        private void FollowTargetWithUi() {
            if(_uiRootTransform == null) {
                return;
            }

            Vector3 worldOffset = defaultUiOffset;
            if(_healthSystem != null && _healthSystem.showSmallHealthBar) {
                worldOffset = new Vector3(
                    _healthSystem.smallHealthBarOffset.x,
                    _healthSystem.smallHealthBarOffset.y + uiYOffsetFromHealthBar,
                    0f);
            }

            _uiRootTransform.position = transform.position + worldOffset;

            if(_cachedMainCamera == null || !_cachedMainCamera.isActiveAndEnabled) {
                _cachedMainCamera = Camera.main;
            }

            if(_cachedMainCamera != null) {
                _uiRootTransform.rotation = _cachedMainCamera.transform.rotation;
            }
        }

        private void RefreshUiView() {
            if(!showConditionProgress) {
                HideUi();
                return;
            }

            EnsureUi();
            if(_uiRoot == null || _healthSystem != null && _healthSystem.isDead) {
                HideUi();
                return;
            }

            bool hasAnyVisibleCondition = false;

            for(int i = 0; i < TrackedConditions.Length; i++) {
                CONDITIONTYPE conditionType = TrackedConditions[i];
                if(!_uiEntries.TryGetValue(conditionType, out ConditionUiEntry uiEntry) || uiEntry == null || uiEntry.Root == null) {
                    continue;
                }

                ConditionState state = GetState(conditionType);
                bool shouldShow = state != null && (state.IsActive || state.Charge > 0f);
                uiEntry.Root.gameObject.SetActive(shouldShow);
                if(!shouldShow) {
                    continue;
                }

                hasAnyVisibleCondition = true;
                Color conditionColor = ConditionColorPalette.GetColor(conditionType);
                float fillValue = state.IsActive ? 1f : Mathf.Clamp01(state.Charge);
                uiEntry.Fill.color = conditionColor;
                uiEntry.Fill.fillAmount = fillValue;
                uiEntry.Icon.enabled = state.IsActive;
                uiEntry.Icon.color = conditionColor;
            }

            _uiRoot.SetActive(hasAnyVisibleCondition);
        }

        private void HideUi() {
            if(_uiRoot != null && _uiRoot.activeSelf) {
                _uiRoot.SetActive(false);
            }
        }

        private void DestroyUiRoot() {
            if(_uiRoot == null || _onApplicationQuit) {
                return;
            }

            Destroy(_uiRoot);
            _uiRoot = null;
            _uiRootTransform = null;
            _uiEntries.Clear();
        }
    }
}
