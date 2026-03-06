// Purpose: Stores normalized per-frame gameplay input state independently from Unity components.
using UnityEngine;
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.Input.Models {

    public sealed class InputStateModel : IModel {

        public Vector2 Move { get; private set; }
        public bool PunchPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool DefendHeld { get; private set; }
        public bool GrabPressedThisFrame { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool DashPressedThisFrame { get; private set; }

        public void SetFrameState(
            Vector2 move,
            bool punchPressedThisFrame,
            bool kickPressedThisFrame,
            bool defendHeld,
            bool grabPressedThisFrame,
            bool jumpPressedThisFrame,
            bool dashPressedThisFrame) {
            Move = move;
            PunchPressedThisFrame = punchPressedThisFrame;
            KickPressedThisFrame = kickPressedThisFrame;
            DefendHeld = defendHeld;
            GrabPressedThisFrame = grabPressedThisFrame;
            JumpPressedThisFrame = jumpPressedThisFrame;
            DashPressedThisFrame = dashPressedThisFrame;
        }
    }
}

