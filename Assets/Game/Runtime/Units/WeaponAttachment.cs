using UnityEngine;

namespace WOR.Gameplay {

    [RequireComponent(typeof(SpriteRenderer))]
    public class WeaponAttachment : MonoBehaviour {

        private SpriteRenderer weaponRenderer;
        private UnitActions unit;
        private Sprite originalSprite; //the original sprite in the scene (when it wasn't picked up)
        private Vector2 spriteOffset;

        public void equipWeapon(WeaponPickup weapon){

            //get components
            unit = GetComponentInParent<UnitActions>();
            weaponRenderer = weapon.GetComponent<SpriteRenderer>();

            //switch to attachment sprite
            originalSprite = weaponRenderer.sprite;
            weaponRenderer.sprite = weapon.spriteAttachment;

            //parent weapon sprite to the unit
            weapon.transform.parent = transform;
            weaponRenderer.flipX = false; //remove any flipped axis
            Vector3 attachedScale = weapon.transform.localScale;
            attachedScale.x = Mathf.Abs(attachedScale.x);
            weapon.transform.localScale = attachedScale;
            weapon.transform.localPosition = weapon.spritePosOffset;

            //save values
            spriteOffset = weapon.spritePosOffset;
            unit.weapon = weapon;
        }

        //destroy current unit's weapon
        public void DestroyWeapon(){

            //play destroy sfx
            if(unit.weapon.destroySFX.Length>0) AudioController.PlaySFX(unit.weapon.destroySFX, transform.position);

            //show destroy effect
            if(unit.weapon.destroyEffect) GameObject.Instantiate(unit.weapon.destroyEffect, transform.position, Quaternion.identity);

            //destroy weapon gameObject
            Destroy(unit.weapon.gameObject);
        }

        //detach weapon from unit and put it back on the floor
        public void DropCurrentWeapon(){
            if(weaponRenderer != null && originalSprite != null) weaponRenderer.sprite = originalSprite; //switch back to original sprite
            unit.weapon.transform.SetParent(null); //reparent weapon to hierarchy root
            unit.weapon.transform.position = unit.transform.position; //put weapon at the position of this unit
            unit.weapon.transform.localRotation = Quaternion.identity;
            Vector3 droppedScale = unit.weapon.transform.localScale;
            float absScaleX = Mathf.Abs(droppedScale.x);
            if(absScaleX <= Mathf.Epsilon) {
                absScaleX = 1f;
            }
            droppedScale.x = unit.dir == DIRECTION.LEFT ? -absScaleX : absScaleX;
            unit.weapon.transform.localScale = droppedScale;
            if(weaponRenderer != null) {
                weaponRenderer.flipX = false;
            }
            unit.weapon = null;
        }

        //units lose a weapon if they get hit or knocked down (depending on the settings)
        public void LoseCurrentWeapon(){
            WeaponPickup wp = unit.GetComponentInChildren<WeaponPickup>();
            DropCurrentWeapon();
            wp?.ShowBounceAnimation();
        }

        void LateUpdate() {

            //do nothing if there is no unit/weapon
            if(!unit || !unit.weapon) return;

            //always make sure that the weapon position and rotation is correct
            unit.weapon.transform.localRotation = Quaternion.identity;
            unit.weapon.transform.localPosition = spriteOffset;           
        }
    }
}

