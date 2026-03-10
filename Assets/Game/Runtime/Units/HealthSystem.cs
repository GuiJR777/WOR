// Purpose: Tracks health, death flow and health-bar visuals while syncing max HP from unit stats.
using System.Collections;
using UnityEngine;
using WOR.Gameplay.Modules.Units.Models;

namespace WOR.Gameplay {

    //healthsystem class for player, enemy and objects
    public class HealthSystem : MonoBehaviour {

	    public int maxHp = 1;
	    public int currentHp = 1;
	    public bool invulnerable;
        public bool isDead => _healthModel != null ? _healthModel.IsDead : currentHp == 0;
        public float healthPercentage => _healthModel != null
            ? _healthModel.HealthRatio
            : (maxHp > 0 ? (float)currentHp / (float)maxHp : 0f);

        [Header("HealthBar Settings")]
        public bool showSmallHealthBar; //small healthbar above the unit
        public Vector2 smallHealthBarOffset = Vector2.zero;
        public bool showLargeHealthBar;//shows a large healthbar, at the bottom of the screen
        private GameObject healthBar;
        private UnitSettings unitSettings;

        [Header("SFX")]
        public string playSFXOnHit = "";
        public string playSFXOnDestroy = "";

        [Header("Effects")]
        public bool showHitFlash = true;
        public float hitFlashDuration = .15f;
        private bool hitflashInProgress;

        [Space(10)]
        public bool showShakeEffect;
        public float shakeIntensity = .08f;
        public float shakeDuration = .5f;
        public float shakeSpeed = 50;

        [Space(10)]
        public GameObject showEffectOnHit;
        public GameObject showEffectOnDestroy;

        public bool isPlayer => gameObject.CompareTag("Player");
        public bool isEnemy => gameObject.CompareTag("Enemy");

        public delegate void OnHealthChange(HealthSystem hs);
	    public static event OnHealthChange onHealthChange;
        public delegate void OnUnitDamaged(HealthSystem hs, int damageAmount, bool isConditionDamage, Color damageColor);
        public static event OnUnitDamaged onUnitDamaged;
        public delegate void OnUnitDeath(GameObject Unit);
	    public static event OnUnitDeath onUnitDeath;
        private HealthModel _healthModel;

        private void OnEnable() {
            unitSettings = GetComponent<UnitSettings>();
            if(unitSettings != null) {
                unitSettings.OnStatsChanged += HandleStatsChanged;
            }
        }

        private void OnDisable() {
            if(unitSettings != null) {
                unitSettings.OnStatsChanged -= HandleStatsChanged;
            }
        }

        private void Start() {
            SyncHealthFromStats(false);
            EnsureHealthModel();

            //if true, create a small health bar above this unit
            if(showSmallHealthBar) CreateSmallHealthbar();

            //initialize player healthbar
            if(isPlayer && onHealthChange != null) onHealthChange(this);
        }

        //create healthbar gameobject and set it into position
        private void CreateSmallHealthbar() {
            if(!healthBar) {
                healthBar = GameObject.Instantiate(Resources.Load("HealthBar")) as GameObject;
                if(healthBar == null) return;
                healthBar.transform.parent = transform;
                healthBar.transform.position = transform.position + (Vector3)smallHealthBarOffset;
                UpdateSmallHealthBarScale();
            }
        }

        //substract health
        public void SubstractHealth(int damage) {
            SubstractHealth(damage, false, Color.white);
        }

        public void SubstractHealth(int damage, bool isConditionDamage, Color damageColor) {

            EnsureHealthModel();
            int healthBeforeDamage = currentHp;
            _healthModel.IsInvulnerable = invulnerable;
            _healthModel.ApplyDamage(damage);
            SyncLegacyFieldsFromModel();
            int appliedDamage = Mathf.Max(0, healthBeforeDamage - currentHp);

            //broadcast Event
		    SendEvent();
            if(appliedDamage > 0 && onUnitDamaged != null) {
                Color popupColor = isConditionDamage ? damageColor : Color.white;
                onUnitDamaged(this, appliedDamage, isConditionDamage, popupColor);
            }

            //update HealthBar
            if(!invulnerable && healthBar) UpdateSmallHealthBarScale();

            //play sfx
            if(currentHp > 0) WOR.Gameplay.AudioController.PlaySFX(playSFXOnHit, transform.position);
            else WOR.Gameplay.AudioController.PlaySFX(playSFXOnDestroy, transform.position);

            //show hitflash
            if(showHitFlash) {
                StartCoroutine(HitFlashRoutine());
            }

            //shake this object
            if(showShakeEffect && !isDead) {
                StopCoroutine(ShakeRoutine());
                StartCoroutine(ShakeRoutine());
            }

            //unit/object health has reached 0
            if(isDead) {

                //show effect on destroy
                if(showEffectOnDestroy) CreateEffect(showEffectOnDestroy);

                if(isEnemy || isPlayer) {

                    //send event
                    if(onUnitDeath != null) onUnitDeath(gameObject);

                } else {

                    //destroy this object
                    Destroy(gameObject);
                }

            } else {

                //show effect when hit
                if(showEffectOnHit) CreateEffect(showEffectOnHit);
            }
	    }

	    //add health
	    public void AddHealth(int amount) {
            EnsureHealthModel();
            _healthModel.Heal(amount);
            SyncLegacyFieldsFromModel();
            UpdateSmallHealthBarScale();
		    SendEvent();
	    }

	    //health update event
	    private void SendEvent() {
		    if(onHealthChange != null) onHealthChange(this);
	    }

        private void HandleStatsChanged() {
            SyncHealthFromStats(true);
        }

        private void SyncHealthFromStats(bool preserveHealthPercentage) {
            if(unitSettings == null) {
                EnsureValidHealth();
                return;
            }

            int newMaxHp = Mathf.Max(1, unitSettings.MaxHpFromStats);
            float healthRatio = preserveHealthPercentage ? GetHealthRatio() : 1f;

            int newCurrentHp = preserveHealthPercentage
                ? Mathf.Clamp(Mathf.RoundToInt(newMaxHp * healthRatio), 0, newMaxHp)
                : Mathf.Clamp(currentHp <= 0 ? newMaxHp : currentHp, 0, newMaxHp);

            if(_healthModel == null) {
                _healthModel = new HealthModel(newMaxHp, newCurrentHp);
            } else {
                _healthModel.SetMaxHp(newMaxHp, newCurrentHp);
            }
            _healthModel.IsInvulnerable = invulnerable;
            SyncLegacyFieldsFromModel();

            UpdateSmallHealthBarScale();
            SendEvent();
        }

        private float GetHealthRatio() {
            if(maxHp <= 0) {
                return 1f;
            }
            return Mathf.Clamp01((float)currentHp / (float)maxHp);
        }

        private void EnsureValidHealth() {
            maxHp = Mathf.Max(1, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
            EnsureHealthModel();
            _healthModel.SetMaxHp(maxHp, currentHp);
            _healthModel.IsInvulnerable = invulnerable;
            SyncLegacyFieldsFromModel();
            UpdateSmallHealthBarScale();
        }

        private void EnsureHealthModel() {
            if(_healthModel != null) {
                return;
            }

            _healthModel = new HealthModel(maxHp, currentHp);
            _healthModel.IsInvulnerable = invulnerable;
        }

        private void SyncLegacyFieldsFromModel() {
            if(_healthModel == null) {
                return;
            }

            maxHp = _healthModel.MaxHp;
            currentHp = _healthModel.CurrentHp;
        }

        private void UpdateSmallHealthBarScale() {
            if(healthBar == null) {
                return;
            }

            Transform fillBar = healthBar.transform.GetChild(0);
            fillBar.localScale = new Vector3(GetHealthRatio(), 1f, 1f);
        }

        //flash white
        private IEnumerator HitFlashRoutine() {
            if(hitflashInProgress) yield break;
            hitflashInProgress = true;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if(sr == null) yield break;
            Material defaultMat = sr.material;

            //change sprite material to hitflash material
            sr.material = Resources.Load("HitFlashMat") as Material;
            yield return new WaitForSeconds(hitFlashDuration);

            //change sprite material back to normal
            sr.material = defaultMat;
            hitflashInProgress = false;
        }

        //shake this object horizontally
        private IEnumerator ShakeRoutine() {
            Vector3 startPos = transform.position;
            float t = 0;
            while(t < 1) {
                transform.position = Vector3.Lerp(startPos + (Vector3.left * shakeIntensity / 2), startPos + (Vector3.right * shakeIntensity / 2), Mathf.Sin(t * shakeSpeed));
                t += Time.deltaTime / shakeDuration;
                yield return 0;
            }
            transform.position = startPos;
        }

        //adjust healthbar positon
        private void OnValidate() {
            EnsureValidHealth();

            if(Application.isPlaying) {
                if(showSmallHealthBar && !healthBar) CreateSmallHealthbar(); //create healthbar if it does not exist
                if(healthBar) healthBar.transform.position = transform.position + (Vector3)smallHealthBarOffset; //update healthbar position
                if(healthBar && !showSmallHealthBar) Destroy(healthBar);
            }
        }

        //show an effect on Destroy
        public void CreateEffect(GameObject effectPrefab) {

            //nothing to show
            if(effectPrefab == null) return;

            //create effect
            Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }
    }
}


