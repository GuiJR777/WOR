// Purpose: Defines serializable combat data used by attacks and combos.
using UnityEngine;
using System.Collections.Generic;

namespace BeatEmUpTemplate2D {

    public enum ATTACKTYPE { NONE, PUNCH, KICK, GROUNDPOUND, GRAB, GRABPUNCH, GRABKICK, GRABTHROW, WEAPON };

    [System.Serializable]
    public class AttackData {

        private const float DEFAULT_KNOCKBACK_FORCE = 1f;
        private const float DEFAULT_KNOCKBACK_DURATION = 0.12f;
        private const float DEFAULT_ATTACKER_FORWARD_DURATION = 0.08f;
        private const float DEFAULT_STRENGTH_DAMAGE_SCALE = 1f;
        private const float MIN_STRENGTH_DAMAGE_SCALE = 0f;
        private const float MAX_STRENGTH_DAMAGE_SCALE = 2f;

        public string name; //optional
        [HideInInspector] public int damage; //legacy fallback damage (kept for backwards compatibility)
        [Range(MIN_STRENGTH_DAMAGE_SCALE, MAX_STRENGTH_DAMAGE_SCALE)]
        public float strengthDamageScale = DEFAULT_STRENGTH_DAMAGE_SCALE; //0..2 percentage of strength converted into damage
        public string animationState = ""; //the animation state, as defined in the Animator component
        public string sfx = ""; //the name of the sfx to be played on hit
        public ATTACKTYPE attackType = ATTACKTYPE.PUNCH;
        public bool knockdown; //if this attack causes a knockDown or not
        public float knockdownLaunchHorizontalForce; //horizontal launch force used for knockdown
        public float knockdownLaunchVerticalForce; //vertical launch force used for knockdown
        public bool applyKnockback = true; //if true, apply a short horizontal knockback on regular hits
        public float knockbackForce = DEFAULT_KNOCKBACK_FORCE; //horizontal pushback force
        public float knockbackVerticalForce; //vertical launch velocity applied on regular hit
        public float knockbackDuration = DEFAULT_KNOCKBACK_DURATION; //duration of the pushback
        public float attackerForwardDistance; //how far the attacker advances at attack start
        public float attackerForwardDuration = DEFAULT_ATTACKER_FORWARD_DURATION; //duration of attacker forward advance
        [HideInInspector] public bool foldout;
        [HideInInspector] public GameObject inflictor; //the gameobject inflicting the damage
    
        public AttackData(string name, int damage, GameObject inflictor, ATTACKTYPE attackType, bool knockdown, string sfx = ""){
            this.name = name;
            this.damage = damage;
            strengthDamageScale = Mathf.Clamp(DEFAULT_STRENGTH_DAMAGE_SCALE, MIN_STRENGTH_DAMAGE_SCALE, MAX_STRENGTH_DAMAGE_SCALE);
            this.inflictor = inflictor;
            this.attackType = attackType;
            this.knockdown = knockdown;
            this.sfx = sfx;
        }

        public float GetStrengthDamageScale() {
            return Mathf.Clamp(strengthDamageScale, MIN_STRENGTH_DAMAGE_SCALE, MAX_STRENGTH_DAMAGE_SCALE);
        }
    }

    [System.Serializable]
    public class Combo {
        public string comboName = "[New Combo]";
        public List<AttackData> attackSequence = new List<AttackData>();
        [HideInInspector] public bool foldout;
    }
}
