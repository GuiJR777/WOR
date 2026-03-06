using UnityEngine;

namespace WOR.Gameplay {

    public class HelpAttribute : PropertyAttribute {
        public string text;
        public HelpAttribute(string text) {
            this.text = text;
        }
    }
}
