using System;
using System.Collections;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using MainProject.Scripts.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MainProject.Scripts.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput _playerInput;

        public Vector2 LeftStickInput => _leftStickInput;
        private Vector2 _leftStickInput;
        
        public Vector2 RightStickInput => _rightStickInput;
        private Vector2 _rightStickInput;

        public KeyState DashKeyState => _dashKeyState;
        private KeyState _dashKeyState = KeyState.off;
        
        public KeyState HealKeyState => _healKeyState;
        private KeyState _healKeyState = KeyState.off;
        
        public KeyState BasicAttackKeyState => _basicAttackKeyState;
        private KeyState _basicAttackKeyState = KeyState.off;

        public KeyState Cast01KeyState => _cast01KeyState;
        private KeyState _cast01KeyState = KeyState.off;
        
        public KeyState Cast02KeyState => _cast02KeyState;
        private KeyState _cast02KeyState = KeyState.off;
        
        public KeyState Cast03KeyState => _cast03KeyState;
        private KeyState _cast03KeyState = KeyState.off;
        
        public KeyState Cast04KeyState => _cast04KeyState;
        private KeyState _cast04KeyState = KeyState.off;
        
        public event Action OnNetStatsPressed;
        public event Action OnStartMenuPressed;
        

        private void OnEnable()
        {
            _playerInput = new PlayerInput();

            SubscribeToInputEvents();
            
            _playerInput.Enable();
        }
        
        private void OnDisable()
        {
            UnsubscribeToInputEvents();
            _playerInput.Disable();
        }

        #region ON_INPUT_CALLBACK_FUNCTIONS
        private void OnNetStats(InputAction.CallbackContext ctx)
        {
            if (PlayerHUDManager.Instance == null) {
                print("PlayerHUD null");
                return;
            }
            
            PlayerHUDManager.Instance.ToggleNetStats();
        }
        
        private void OnMoveInput(InputAction.CallbackContext ctx)
        {
            _leftStickInput = ctx.ReadValue<Vector2>();
        }
        
        private void OnRightStick(InputAction.CallbackContext ctx)
        {
            _rightStickInput = ctx.ReadValue<Vector2>();
        }
        
        private void OnDash(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _dashKeyState, ctx);
        }
        
        private void OnHeal(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _healKeyState, ctx);
        }
        
        private void OnBasicAttack(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _basicAttackKeyState, ctx);
        }

        private void OnCast01(InputAction.CallbackContext ctx)
        {
           HandleKeyState(ref _cast01KeyState, ctx);
        }
        
        private void OnCast02(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _cast02KeyState, ctx);
        }
        
        private void OnCast03(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _cast03KeyState, ctx);
        }
        
        private void OnCast04(InputAction.CallbackContext ctx)
        {
            HandleKeyState(ref _cast04KeyState, ctx);
        }
        
        private void OnStartMenu(InputAction.CallbackContext ctx)
        {
            print("Start Invoke");
            OnStartMenuPressed?.Invoke();
        }
        #endregion
        
        /// <summary>
        /// Moves the button to the next state, after it has been sent to the server
        /// </summary>
        public void ClearButtonInput()
        {
            StepKeyState(ref _dashKeyState);
            StepKeyState(ref _healKeyState);
            StepKeyState(ref _basicAttackKeyState);
            StepKeyState(ref _cast01KeyState);
            StepKeyState(ref _cast02KeyState);
            StepKeyState(ref _cast03KeyState);
            StepKeyState(ref _cast04KeyState);
        }
        
        /// <summary>
        /// Handles the KeyState of a button passed by reference
        /// </summary>
        /// <param name="button">Reference of button</param>
        /// <param name="ctx">CallbackContext of button</param>
        private void HandleKeyState(ref KeyState button, InputAction.CallbackContext ctx)
        {
            if (ctx.started && button == KeyState.off) {
                button = KeyState.press;
            }
            
            if (ctx.performed && button == KeyState.press || button == KeyState.held) {
                button = KeyState.held;
            }
            
            if (ctx.canceled && button == KeyState.held) {
                button = KeyState.release;
            }
        }
        
        /// <summary>
        /// Pass in button by reference to step KeyState for next frame
        /// </summary>
        /// <param name="button"></param>
        private void StepKeyState(ref KeyState button)
        {
            switch (button)
            {
                case KeyState.press: 
                case KeyState.held:
                    button = KeyState.held;
                    break;
                case KeyState.release:
                    button = KeyState.off;
                    break;
            }
        }

        public void RumbleController(float time, float amount)
        {
            StartCoroutine(RumbleControllerOverTime(time, amount)); 
        }
        
        private IEnumerator RumbleControllerOverTime(float time, float amount)
        {
            InputHelpers.SetControllerVibration(amount, amount);
            yield return new WaitForSeconds(time);
            InputHelpers.ResetCurrentHaptics();
        }

        private void SubscribeToInputEvents()
        {
            _playerInput.CharacterControls.Move.started += OnMoveInput; 
            _playerInput.CharacterControls.Move.performed += OnMoveInput; 
            _playerInput.CharacterControls.Move.canceled += OnMoveInput;
            
            _playerInput.CharacterControls.RightStick.started += OnRightStick; 
            _playerInput.CharacterControls.RightStick.performed += OnRightStick; 
            _playerInput.CharacterControls.RightStick.canceled += OnRightStick;
            
            _playerInput.CharacterControls.Dash.started += OnDash;
            _playerInput.CharacterControls.Dash.performed += OnDash;
            _playerInput.CharacterControls.Dash.canceled += OnDash;
            
            _playerInput.CharacterControls.Heal.started += OnHeal;
            _playerInput.CharacterControls.Heal.performed += OnHeal;
            _playerInput.CharacterControls.Heal.canceled += OnHeal;

            _playerInput.CharacterControls.NetStatsMonitor.performed += OnNetStats;

            _playerInput.CharacterControls.BasicAttack.started += OnBasicAttack;
            _playerInput.CharacterControls.BasicAttack.performed += OnBasicAttack;
            _playerInput.CharacterControls.BasicAttack.canceled += OnBasicAttack;
            
            _playerInput.CharacterControls.Cast01.started += OnCast01;
            _playerInput.CharacterControls.Cast01.performed += OnCast01;
            _playerInput.CharacterControls.Cast01.canceled += OnCast01;
            
            _playerInput.CharacterControls.Cast02.started += OnCast02;
            _playerInput.CharacterControls.Cast02.performed += OnCast02;
            _playerInput.CharacterControls.Cast02.canceled += OnCast02;
            
            _playerInput.CharacterControls.Cast03.started += OnCast03;
            _playerInput.CharacterControls.Cast03.performed += OnCast03;
            _playerInput.CharacterControls.Cast03.canceled += OnCast03;
            
            _playerInput.CharacterControls.Cast04.started += OnCast04;
            _playerInput.CharacterControls.Cast04.performed += OnCast04;
            _playerInput.CharacterControls.Cast04.canceled += OnCast04;
            
            _playerInput.CharacterControls.NetStatsMonitor.performed += OnNetStats;

        }

        private void UnsubscribeToInputEvents()
        {
            _playerInput.CharacterControls.Move.started -= OnMoveInput; 
            _playerInput.CharacterControls.Move.performed -= OnMoveInput; 
            _playerInput.CharacterControls.Move.canceled -= OnMoveInput;
            
            _playerInput.CharacterControls.RightStick.started -= OnRightStick; 
            _playerInput.CharacterControls.RightStick.performed -= OnRightStick; 
            _playerInput.CharacterControls.RightStick.canceled -= OnRightStick;
            
            _playerInput.CharacterControls.NetStatsMonitor.performed -= OnNetStats;
            
            _playerInput.CharacterControls.Dash.started -= OnDash;
            _playerInput.CharacterControls.Dash.performed -= OnDash;
            _playerInput.CharacterControls.Dash.canceled -= OnDash;
            
            _playerInput.CharacterControls.Heal.started -= OnHeal;
            _playerInput.CharacterControls.Heal.performed -= OnHeal;
            _playerInput.CharacterControls.Heal.canceled -= OnHeal;
            
            _playerInput.CharacterControls.BasicAttack.started -= OnBasicAttack;
            _playerInput.CharacterControls.BasicAttack.performed -= OnBasicAttack;
            _playerInput.CharacterControls.BasicAttack.canceled -= OnBasicAttack;

            _playerInput.CharacterControls.Cast01.started -= OnCast01;
            _playerInput.CharacterControls.Cast01.performed -= OnCast01;
            _playerInput.CharacterControls.Cast01.canceled -= OnCast01;
            
            _playerInput.CharacterControls.Cast02.started -= OnCast02;
            _playerInput.CharacterControls.Cast02.performed -= OnCast02;
            _playerInput.CharacterControls.Cast02.canceled -= OnCast02;
            
            _playerInput.CharacterControls.Cast03.started -= OnCast03;
            _playerInput.CharacterControls.Cast03.performed -= OnCast03;
            _playerInput.CharacterControls.Cast03.canceled -= OnCast03;
            
            _playerInput.CharacterControls.Cast04.started -= OnCast04;
            _playerInput.CharacterControls.Cast04.performed -= OnCast04;
            _playerInput.CharacterControls.Cast04.canceled -= OnCast04;
        }

    }
}