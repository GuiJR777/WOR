// Purpose: Tracks health, death flow and health-bar visuals while syncing max HP from unit stats.
using System.Collections;
using UnityEngine;

namespace BeatEmUpTemplate2D {

    //healthsystem class for player, enemy and objects
    public class HealthSystem : MonoBehaviour {

	    public int maxHp = 1;
	    public int currentHp = 1;
	    public bool invulnerable;
        public bool isDead => (currentHp == 0);
        public float healthPercentage => maxHp > 0 ? (float)currentHp / (float)maxHp : 0f;

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
        public delegate void OnUnitDeath(GameObject Unit);
	    public static event OnUnitDeath onUnitDeath;

        private void OnEnable() {
            unitSettings = GetComponent<UnitSettings>();
            if(unitSettings != null) {
                unitSettings.OnStatsChanged += HandleStatsChanged;
            }

            //add enemies to enemyList
            if(isEnemy) EnemyManager.AddEnemyToList(gameObject);
        }

        private void OnDisable() {
            if(unitSettings != null) {
                unitSettings.OnStatsChanged -= HandleStatsChanged;
            }

            //remove enemies from enemyList
            if(isEnemy) EnemyManager.RemoveEnemyFromList(gameObject);
        }

        private void Start() {
            SyncHealthFromStats(false);

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

		    //reduce hp
		    if(!invulnerable) currentHp = Mathf.Clamp(currentHp -= damage, 0, maxHp);

            //broadcast Event
		    SendEvent();

            //update HealthBar
            if(!invulnerable && healthBar) UpdateSmallHealthBarScale();

            //play sfx
            if(currentHp > 0) BeatEmUpTemplate2D.AudioController.PlaySFX(playSFXOnHit, transform.position);
            else BeatEmUpTemplate2D.AudioController.PlaySFX(playSFXOnDestroy, transform.position);

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
		    currentHp = Mathf.Clamp(currentHp += amount, 0, maxHp);
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

            maxHp = newMaxHp;
            if(preserveHealthPercentage) {
                currentHp = Mathf.Clamp(Mathf.RoundToInt(maxHp * healthRatio), 0, maxHp);
            } else {
                currentHp = Mathf.Clamp(currentHp <= 0 ? maxHp : currentHp, 0, maxHp);
            }

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
            UpdateSmallHealthBarScale();
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

