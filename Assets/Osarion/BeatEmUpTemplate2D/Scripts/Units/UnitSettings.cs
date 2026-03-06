// Purpose: Stores unit configuration and centralizes all runtime stat calculations and modifiers.
using System.Collections.Generic;
using UnityEngine;

namespace BeatEmUpTemplate2D {

    public enum UNITTYPE { PLAYER = 0, ENEMY = 10, NPC = 20 }
    public enum UNITFACTION { NEUTRAL = 0, HERO = 10, ENEMY = 20, ALLY = 30, BOSS = 40 }
    public enum UNITSTATTYPE { CONSTITUTION = 0, CHAKRA = 10, STRENGTH = 20, DEFENSE = 30, AGILITY = 40, LUCK = 50 }

    [System.Serializable]
    public class UnitSettings : MonoBehaviour {

        private const float DEFAULT_DEPTH_MULTIPLIER = 0.8f;
        private const float DEFAULT_PARRY_WINDOW = 0.2f;
        private const float DEFAULT_PARRY_STUN_DURATION = 0.4f;
        private const float DEFAULT_PARRY_KNOCKBACK_FORCE = 1.75f;
        private const float DEFAULT_PARRY_KNOCKBACK_DURATION = 0.15f;
        private const float DEFAULT_HIT_KNOCKBACK_FORCE = 0.9f;
        private const float DEFAULT_HIT_KNOCKBACK_DURATION = 0.12f;
        private const float DEFAULT_DASH_SPEED = 11f;
        private const float DEFAULT_DASH_DURATION = 0.2f;
        private const float DEFAULT_DASH_COOLDOWN = 0.4f;
        private const float DEFAULT_DASH_GHOST_INTERVAL = 0.04f;
        private const float DEFAULT_STAT_MULTIPLIER = 1f;
        private const float DEFAULT_CONSTITUTION = 10f;
        private const float DEFAULT_CHAKRA = 0f;
        private const float DEFAULT_STRENGTH = 10f;
        private const float DEFAULT_DEFENSE = 0f;
        private const float DEFAULT_AGILITY = 4f;
        private const float DEFAULT_LUCK = 0f;
        private const float HEALTH_PER_CONSTITUTION = 10f;
        private const float AIR_MOVE_SPEED_FACTOR = 0.8f;
        private const float MIN_STAT_VALUE = 0f;
        private const float MIN_STAT_MULTIPLIER = 0f;

        private sealed class RuntimeStatModifier {
            public string sourceId;
            public float additive;
            public float multiplier = DEFAULT_STAT_MULTIPLIER;
        }

        public event System.Action OnStatsChanged;

        public UNITTYPE unitType = UNITTYPE.PLAYER;

        //UNIT PROFILE
        public UNITFACTION faction = UNITFACTION.NEUTRAL;
        public int unitLevel = 1;
        public string unitRole = "";
        public bool canDetect = true;
        public bool canBeDetected = true;

        //CORE STATS
        public float constitution = DEFAULT_CONSTITUTION;
        public float constitutionMultiplier = DEFAULT_STAT_MULTIPLIER;
        public float chakra = DEFAULT_CHAKRA;
        public float chakraMultiplier = DEFAULT_STAT_MULTIPLIER;
        public float strength = DEFAULT_STRENGTH;
        public float strengthMultiplier = DEFAULT_STAT_MULTIPLIER;
        public float defense = DEFAULT_DEFENSE;
        public float defenseMultiplier = DEFAULT_STAT_MULTIPLIER;
        public float agility = DEFAULT_AGILITY;
        public float agilityMultiplier = DEFAULT_STAT_MULTIPLIER;
        public float luck = DEFAULT_LUCK;
        public float luckMultiplier = DEFAULT_STAT_MULTIPLIER;

        //LINKED OBJECTS
        public GameObject shadowPrefab; //shadow prefab
        public GameObject shadow; //shadow that follows this unit
        public GameObject weaponBone; //position for weapon attachments
        public GameObject hitEffect; //effect that gets played when we've hit something
        public SpriteRenderer hitBox; //sprite bounding box used for hit collision
        public SpriteRenderer spriteRenderer; //this unit's sprite renderer

        //MOVEMENT SETTINGS
        public DIRECTION startDirection = DIRECTION.RIGHT; //start direction
        public float depthMoveMultiplier = DEFAULT_DEPTH_MULTIPLIER; //z-axis movement multiplier for depth feel
        public bool useAcceleration = false; //use acceleration over time if true, or move instantly when false

        //ACCELERATION / DECELERATION
        public float moveAcceleration = 25f; //how fast we gain speed
        public float moveDeceleration = 10f; //how fast we lose speed

        //JUMP SETTINGS
        public float jumpHeight = 4; //how high this unit can jump
        public float jumpSpeed = 3.5f; //how fast the jump is simulated
        public float jumpGravity = 5f; //the downward force

        //ATTACK DATA
        [Space(10)]
        [Help("* Only PUNCH and KICK Attack Types can be used in combos")]
        public List<Combo> comboData = new List<Combo>();
        public float comboResetTime = .55f; //when this time expires a combo will be reset
        public bool continueComboOnHit; //only continue the combo when the previous attack hit a target
        [Space(10)]
        public AttackData jumpPunch;
        public AttackData jumpKick;
        [Space(10)]
        public AttackData grabPunch;
        public AttackData grabKick;
        public AttackData grabThrow;
        [Space(10)]
        public AttackData groundPunch;
        public AttackData groundKick;

        //ENEMY ATTACK DATA
        public List<AttackData> enemyAttackList = new List<AttackData>(); //list of enemy attacks

        //KNOCKDOWN SETTINGS
        public bool canBeKnockedDown = true; //if this unit can be knocked down
        public float knockDownHeight = 3; //how high the unit flies in the air during a knockdown
        public float knockDownDistance = 3; //horizontal movement distance
        public float knockDownSpeed = 3; //how fast the knockdown simulates
        public float knockDownFloorTime = 1; //how long this unit will stay on the floor before standing up
        public bool hitOtherEnemiesWhenFalling = false; //option to hit other enemies when falling down

        //THROW SETTINGS
        public float throwHeight = 3; //how high this unit flies when thrown
        public float throwDistance = 5; //how far this unit is thrown
        public bool hitOtherEnemiesWhenThrown = true; //option to hit other enemies when being thrown by the player

        //DEFENCE SETTINGS
        public float defendChance; //a percentage change that an enemy defends an incoming attack
        public float defendDuration; //how long an enemy stays in defend state
        public bool canChangeDirWhileDefending; //enable/disable changing direction while defending
        public bool rearDefenseEnabled; //can defend attacks coming from behind while defending
        public float parryWindow = DEFAULT_PARRY_WINDOW; //perfect defend window after entering defend state
        public float parryStunDuration = DEFAULT_PARRY_STUN_DURATION; //stun duration applied to attacker on successful parry
        public float parryKnockbackForce = DEFAULT_PARRY_KNOCKBACK_FORCE; //pushback force applied to attacker on successful parry
        public float parryKnockbackDuration = DEFAULT_PARRY_KNOCKBACK_DURATION; //pushback duration applied to attacker on successful parry
        public float hitKnockbackForce = DEFAULT_HIT_KNOCKBACK_FORCE; //default pushback force on regular hit
        public float hitKnockbackDuration = DEFAULT_HIT_KNOCKBACK_DURATION; //default pushback duration on regular hit

        //GRAB SETTINGS
        public bool canBeGrabbed = true;
        public string grabAnimation = "Grab";
        public Vector2 grabPosition = new Vector2(0.93f, 0);
        public float grabDuration = 3f;

        //EQUIPPED WEAPON SETTINGS
        public bool loseWeaponWhenHit = true;
        public bool loseWeaponWhenKnockedDown = true;

        //DASH SETTINGS
        public bool canDash = true;
        public float dashSpeed = DEFAULT_DASH_SPEED;
        public float dashDuration = DEFAULT_DASH_DURATION;
        public float dashCooldown = DEFAULT_DASH_COOLDOWN;
        public float dashGhostInterval = DEFAULT_DASH_GHOST_INTERVAL;
        public bool dashInvulnerable = true;

        //UNIT NAME AND PORTRAIT
        public int playerId = 1; //player reference Id
        public string unitName = ""; //the name of this unit
        public bool showNameInAllCaps; //this the name in capital letters
        public Sprite unitPortrait; //small unit portrait next to the healtbar
        public bool loadRandomNameFromList; //true if you want to load a random name from a txt file
        public TextAsset unitNamesList; //list of names (.txt file)

        //ENEMY SETTINGS
        public float enemyPauseBeforeAttack = .3f; //timeout before enemy attacks the target

        //FIELD OF VIEW
        public bool enableFOV; //use FOV to spot the target, when false the target is always spotted by default
        public float viewDistance = 5f; // The maximum distance the GameObject can see
        public float viewAngle = 45f; // The angle of the field of view
        public Vector2 viewPosOffset; //the view cone offset in x/z
        public float viewHeightOffset; //the view cone height offset on y axis
        public bool showFOVCone; //show the FOV cone in the Unity Editor
        [ReadOnlyProperty] public bool targetInSight; //true if the target is in the field of view of this enemy

        private readonly Dictionary<UNITSTATTYPE, List<RuntimeStatModifier>> _runtimeStatModifiers =
            new Dictionary<UNITSTATTYPE, List<RuntimeStatModifier>>();

        private UnitActions unitActions => GetComponent<UnitActions>();

        public float MoveSpeedFromStats {
            get {
                return Mathf.Max(0f, GetAgility());
            }
        }

        public float MoveSpeedAirFromStats {
            get {
                return MoveSpeedFromStats * AIR_MOVE_SPEED_FACTOR;
            }
        }

        public int MaxHpFromStats => Mathf.Max(1, Mathf.RoundToInt(GetConstitution() * HEALTH_PER_CONSTITUTION));

        public float GetConstitution() {
            return GetStatValue(UNITSTATTYPE.CONSTITUTION);
        }

        public float GetChakra() {
            return GetStatValue(UNITSTATTYPE.CHAKRA);
        }

        public float GetStrength() {
            return GetStatValue(UNITSTATTYPE.STRENGTH);
        }

        public float GetDefense() {
            return GetStatValue(UNITSTATTYPE.DEFENSE);
        }

        public float GetAgility() {
            return GetStatValue(UNITSTATTYPE.AGILITY);
        }

        public float GetLuck() {
            return GetStatValue(UNITSTATTYPE.LUCK);
        }

        public float GetCriticalChance() {
            return Mathf.Clamp01(GetLuck() / 100f);
        }

        public float GetStatValue(UNITSTATTYPE statType) {
            float baseValue = Mathf.Max(MIN_STAT_VALUE, GetBaseStatValue(statType));
            float configuredMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, GetBaseStatMultiplier(statType));

            float additiveBonus = 0f;
            float runtimeMultiplier = 1f;

            if(_runtimeStatModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                for(int i = 0; i < modifiers.Count; i++) {
                    RuntimeStatModifier modifier = modifiers[i];
                    if(modifier == null) {
                        continue;
                    }
                    additiveBonus += modifier.additive;
                    runtimeMultiplier *= Mathf.Max(0f, modifier.multiplier);
                }
            }

            float configuredValue = baseValue * configuredMultiplier;
            float finalValue = (configuredValue + additiveBonus) * runtimeMultiplier;
            return Mathf.Max(0f, finalValue);
        }

        public void SetStatModifier(UNITSTATTYPE statType, string sourceId, float additive, float multiplier = 1f) {
            if(string.IsNullOrEmpty(sourceId)) {
                Debug.LogWarning("SetStatModifier ignored because sourceId is empty.", this);
                return;
            }

            if(!_runtimeStatModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                modifiers = new List<RuntimeStatModifier>();
                _runtimeStatModifiers.Add(statType, modifiers);
            }

            RuntimeStatModifier existingModifier = null;
            for(int i = 0; i < modifiers.Count; i++) {
                RuntimeStatModifier modifier = modifiers[i];
                if(modifier != null && modifier.sourceId == sourceId) {
                    existingModifier = modifier;
                    break;
                }
            }

            if(existingModifier == null) {
                existingModifier = new RuntimeStatModifier();
                existingModifier.sourceId = sourceId;
                modifiers.Add(existingModifier);
            }

            existingModifier.additive = additive;
            existingModifier.multiplier = Mathf.Max(0f, multiplier);
            NotifyStatsChanged();
        }

        public bool RemoveStatModifier(UNITSTATTYPE statType, string sourceId) {
            if(string.IsNullOrEmpty(sourceId)) {
                return false;
            }

            if(!_runtimeStatModifiers.TryGetValue(statType, out List<RuntimeStatModifier> modifiers)) {
                return false;
            }

            for(int i = modifiers.Count - 1; i >= 0; i--) {
                RuntimeStatModifier modifier = modifiers[i];
                if(modifier != null && modifier.sourceId == sourceId) {
                    modifiers.RemoveAt(i);
                    NotifyStatsChanged();
                    return true;
                }
            }
            return false;
        }

        public int RemoveAllStatModifiersFromSource(string sourceId) {
            if(string.IsNullOrEmpty(sourceId)) {
                return 0;
            }

            int removedCount = 0;
            foreach(KeyValuePair<UNITSTATTYPE, List<RuntimeStatModifier>> pair in _runtimeStatModifiers) {
                List<RuntimeStatModifier> modifiers = pair.Value;
                for(int i = modifiers.Count - 1; i >= 0; i--) {
                    RuntimeStatModifier modifier = modifiers[i];
                    if(modifier != null && modifier.sourceId == sourceId) {
                        modifiers.RemoveAt(i);
                        removedCount++;
                    }
                }
            }

            if(removedCount > 0) {
                NotifyStatsChanged();
            }
            return removedCount;
        }

        public void ClearStatModifiers() {
            _runtimeStatModifiers.Clear();
            NotifyStatsChanged();
        }

        private float GetBaseStatValue(UNITSTATTYPE statType) {
            switch(statType) {
                case UNITSTATTYPE.CONSTITUTION:
                    return constitution;
                case UNITSTATTYPE.CHAKRA:
                    return chakra;
                case UNITSTATTYPE.STRENGTH:
                    return strength;
                case UNITSTATTYPE.DEFENSE:
                    return defense;
                case UNITSTATTYPE.AGILITY:
                    return agility;
                case UNITSTATTYPE.LUCK:
                    return luck;
                default:
                    return 0f;
            }
        }

        private float GetBaseStatMultiplier(UNITSTATTYPE statType) {
            switch(statType) {
                case UNITSTATTYPE.CONSTITUTION:
                    return constitutionMultiplier;
                case UNITSTATTYPE.CHAKRA:
                    return chakraMultiplier;
                case UNITSTATTYPE.STRENGTH:
                    return strengthMultiplier;
                case UNITSTATTYPE.DEFENSE:
                    return defenseMultiplier;
                case UNITSTATTYPE.AGILITY:
                    return agilityMultiplier;
                case UNITSTATTYPE.LUCK:
                    return luckMultiplier;
                default:
                    return DEFAULT_STAT_MULTIPLIER;
            }
        }

        private void NotifyStatsChanged() {
            OnStatsChanged?.Invoke();
        }

        private void ClampStatConfiguration() {
            unitLevel = Mathf.Max(1, unitLevel);

            constitution = Mathf.Max(MIN_STAT_VALUE, constitution);
            chakra = Mathf.Max(MIN_STAT_VALUE, chakra);
            strength = Mathf.Max(MIN_STAT_VALUE, strength);
            defense = Mathf.Max(MIN_STAT_VALUE, defense);
            agility = Mathf.Max(MIN_STAT_VALUE, agility);
            luck = Mathf.Max(MIN_STAT_VALUE, luck);

            constitutionMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, constitutionMultiplier);
            chakraMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, chakraMultiplier);
            strengthMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, strengthMultiplier);
            defenseMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, defenseMultiplier);
            agilityMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, agilityMultiplier);
            luckMultiplier = Mathf.Max(MIN_STAT_MULTIPLIER, luckMultiplier);
        }

        private void Start() {

            ClampStatConfiguration();

            //create shadow
            if(!shadow && shadowPrefab) shadow = GameObject.Instantiate(shadowPrefab, transform.parent) as GameObject;

            //hide hitbox at start
            if(hitBox) hitBox.color = Color.clear;
            else Debug.LogError("Please assign a HitBox to GameObject " + gameObject.name + " in UnitSettings/Linked Components");

            //check sprite renderer
            if(spriteRenderer == null) Debug.Log("Please assign a SpriteRenderer to GameObject " + gameObject.name + " in UnitSettings/Linked Components");

            //load name
            if(loadRandomNameFromList) unitName = GetRandomName();
        }

        private void Update() {

            //Show hitbox debug info in Unity Editor
            #if UNITY_EDITOR
                if(hitBox && hitBox.gameObject.activeSelf) MathUtilities.DrawRectGizmo(hitBox.bounds.center, hitBox.bounds.size, Color.red, Time.deltaTime);
            #endif

            //let blobshadow follow this unit
            if(shadow && unitActions != null) {
                shadow.transform.position = new Vector3(transform.position.x, unitActions.baseHeight, unitActions.groundPos);
            }

            //target in FOV
            targetInSight = unitActions != null ? unitActions.targetInSight() : false;
        }

        //returns a random name
	    private string GetRandomName() {

		    if(unitNamesList == null) {
			    Debug.Log("no list of unit names was found, please create a .txt file with names on each line, and link it in the unitSettings component.");
			    return "";
		    }

		    //convert the lines of the txt file to an array
		    string data = unitNamesList.ToString();
		    string cReturns = System.Environment.NewLine + "\n" + "\r";
		    string[] lines = data.Split(cReturns.ToCharArray());

		    //pick a random name from the list
		    string name = "";
		    int cnt = 0;
		    while(name.Length == 0 && cnt < 100) {
			    int rand = Random.Range(0, lines.Length);
			    name = lines[rand];
			    cnt += 1;
		    }
		    return name;
	    }

        //show start direction in Unity Editor
        private void OnValidate() {
            ClampStatConfiguration();
            transform.localRotation = (startDirection == DIRECTION.LEFT) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            if(Application.isPlaying) {
                NotifyStatsChanged();
            }
        }
    }

    public static class CombatDamageCalculator {

        private const float DAMAGE_CRITICAL_MULTIPLIER = 3f;

        public static int CalculateDamage(
            AttackData attackData,
            UnitSettings attackerSettings,
            UnitSettings defenderSettings,
            out bool criticalHit) {
            criticalHit = false;

            if(attackData == null) {
                return 0;
            }

            //fallback to legacy flat damage when attacker has no stats source
            if(attackerSettings == null) {
                return Mathf.Max(0, attackData.damage);
            }

            float attackScale = attackData.GetStrengthDamageScale();
            float attackerStrength = attackerSettings.GetStrength();
            float rawDamage = Mathf.Max(0f, attackerStrength * attackScale);

            float defenderDefense = defenderSettings != null ? defenderSettings.GetDefense() : 0f;
            float reducedDamage = Mathf.Max(0f, rawDamage - defenderDefense);

            float criticalChance = attackerSettings.GetCriticalChance();
            if(criticalChance > 0f && Random.value <= criticalChance) {
                criticalHit = true;
            }

            float finalDamage = criticalHit ? reducedDamage * DAMAGE_CRITICAL_MULTIPLIER : reducedDamage;
            return Mathf.Max(0, Mathf.RoundToInt(finalDamage));
        }

        public static int CalculateDamageFromInflictor(
            AttackData attackData,
            GameObject inflictor,
            UnitSettings defenderSettings,
            out bool criticalHit) {
            UnitSettings attackerSettings = inflictor != null ? inflictor.GetComponent<UnitSettings>() : null;
            return CalculateDamage(attackData, attackerSettings, defenderSettings, out criticalHit);
        }
    }
}
