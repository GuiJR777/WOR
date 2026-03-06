using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using WOR.Gameplay.Modules.Input.Models;

namespace WOR.Gameplay {

    // Purpose: Centralizes gameplay input access through Unity Input System actions.
    //Input Manager for the Modern Unity Input System
    public class InputManager : MonoBehaviour {

        public static InputManager Instance { get; private set; }
        [Header("MODERN INPUTMANAGER. v1.0")]
        [ReadOnlyProperty] public string controlsScheme;
        public PlayerControls playerInput;
        private InputAction move;
        private InputAction punch;
        private InputAction kick;
        private InputAction defend;
        private InputAction grab;
        private InputAction jump;
        private InputAction dash;
        private InputStateModel inputStateModel;

        void Awake(){

            playerInput = new PlayerControls();
            controlsScheme = playerInput.ToString();
            inputStateModel = new InputStateModel();

            //singleton pattern (only one InputManager allowed in a scene)
            if (Instance == null) {
                Instance = this;

            } else {
                Debug.Log("Multiple InputManagers found in this scene, there can be only one.");
                Destroy(gameObject);
            }
	    }

        void OnEnable(){

            //subscribe to event
            InputSystem.onDeviceChange += OnDeviceChange;

            move = playerInput.Player.Move;
            punch = playerInput.Player.Punch;
            kick = playerInput.Player.Kick;
            defend = playerInput.Player.Defend;
            grab = playerInput.Player.Grab;
            jump = playerInput.Player.Jump;
            dash = playerInput.Player.Dash;

            move.Enable();
            punch.Enable();
            kick.Enable();
            defend.Enable();
            grab.Enable();
            jump.Enable();
            dash.Enable();
        }

        void OnDisable(){

            //unsubscribe from event
            InputSystem.onDeviceChange -= OnDeviceChange;

            move.Disable();
            punch.Disable();
            kick.Disable();
            defend.Disable();
            grab.Disable();
            jump.Disable();
            dash.Disable();
        }

        void Update() {
            if(inputStateModel == null) {
                return;
            }

            inputStateModel.SetFrameState(
                move != null ? move.ReadValue<Vector2>() : Vector2.zero,
                punch != null && punch.WasPressedThisFrame(),
                kick != null && kick.WasPressedThisFrame(),
                defend != null && defend.IsPressed(),
                grab != null && grab.WasPressedThisFrame(),
                jump != null && jump.WasPressedThisFrame(),
                dash != null && dash.WasPressedThisFrame());
        }

        //get Punch button state
        public static bool PunchKeyDown(int playerId){
            return Instance?.inputStateModel?.PunchPressedThisFrame ?? false;
        }

        //get Kick button state
        public static bool KickKeyDown(int playerId){
            return Instance?.inputStateModel?.KickPressedThisFrame ?? false;
        }

        //get Jump button state
        public static bool DefendKeyDown(int playerId){
            return Instance?.inputStateModel?.DefendHeld ?? false;
        }

        //get Grab button state
        public static bool GrabKeyDown(int playerId){
            return Instance?.inputStateModel?.GrabPressedThisFrame ?? false;
        }

        //get Jump button state
        public static bool JumpKeyDown(int playerId){
            return Instance?.inputStateModel?.JumpPressedThisFrame ?? false;
        }

        //get Dash button state
        public static bool DashKeyDown(int playerId){
            return Instance?.inputStateModel?.DashPressedThisFrame ?? false;
        }

        //returns the directional input as a vector2
        public static Vector2 GetInputVector(int playerId){
            return Instance?.inputStateModel?.Move ?? Vector2.zero;
        }

        //detect joypad direction input
        public static bool JoypadDirInputDetected(int playerId){
            Vector2 moveVector = Instance?.inputStateModel?.Move ?? Vector2.zero;
            return moveVector.x != 0f || moveVector.y != 0f;
        }

        //detect device input change
        void OnDeviceChange(InputDevice device, InputDeviceChange change) {
            if (change == InputDeviceChange.Added) {
                if(InputUser.all.Count > 0) {
                    InputUser.PerformPairingWithDevice(device, InputUser.all[0], InputUserPairingOptions.ForceNoPlatformUserAccountSelection);
                }
            } else if (change == InputDeviceChange.Removed) {
            }
        }
    }
}

