using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace BeatEmUpTemplate2D {

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

        void Awake(){

            playerInput = new PlayerControls();
            controlsScheme = playerInput.ToString();

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

        //get Punch button state
        public static bool PunchKeyDown(int playerId){
            return Instance?.punch?.WasPressedThisFrame()?? false;
        }

        //get Kick button state
        public static bool KickKeyDown(int playerId){
            return Instance?.kick?.WasPressedThisFrame()?? false;
        }

        //get Jump button state
        public static bool DefendKeyDown(int playerId){
            return Instance?.defend?.IsPressed()?? false;
        }

        //get Grab button state
        public static bool GrabKeyDown(int playerId){
            return Instance?.grab?.WasPressedThisFrame()?? false;
        }

        //get Jump button state
        public static bool JumpKeyDown(int playerId){
            return Instance?.jump?.WasPressedThisFrame()?? false;
        }

        //get Dash button state
        public static bool DashKeyDown(int playerId){
            return Instance?.dash?.WasPressedThisFrame()?? false;
        }

        //returns the directional input as a vector2
        public static Vector2 GetInputVector(int playerId){
            return Instance?.move?.ReadValue<Vector2>()?? Vector2.zero;
        }

        //detect joypad direction input
        public static bool JoypadDirInputDetected(int playerId){
            return (Instance?.move?.ReadValue<Vector2>().x != 0 || Instance?.move?.ReadValue<Vector2>().y != 0);
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
