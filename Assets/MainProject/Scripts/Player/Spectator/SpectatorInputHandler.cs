using UnityEngine;
using UnityEngine.InputSystem;

namespace MainProject.Scripts.Player.Spectator
{
    public class SpectatorInputHandler : MonoBehaviour
    {
        private PlayerInput _playerInput;

        public Vector2 LeftStickInput => _leftStickInput;
        private Vector2 _leftStickInput;
        
        public Vector2 RightStickInput => _rightStickInput;
        private Vector2 _rightStickInput;

        public bool BoostPressed;
        public bool UpPressed;
        public bool DownPressed;
    
        private void OnEnable()
        {
            _playerInput = new PlayerInput();

            _playerInput.CameraController.Move.started += OnLeftStickInput;
            _playerInput.CameraController.Move.performed += OnLeftStickInput;
            _playerInput.CameraController.Move.canceled += OnLeftStickInput;
        
            _playerInput.CameraController.RightStick.started += OnRightInput;
            _playerInput.CameraController.RightStick.performed += OnRightInput;
            _playerInput.CameraController.RightStick.canceled += OnRightInput;
        
            _playerInput.CameraController.Boost.started += OnBoost;
            _playerInput.CameraController.Boost.performed += OnBoost;
            _playerInput.CameraController.Boost.canceled += OnBoost;
        
            _playerInput.CameraController.Up.started += OnUp;
            _playerInput.CameraController.Up.performed += OnUp;
            _playerInput.CameraController.Up.canceled += OnUp;
        
            _playerInput.CameraController.Down.started += OnDown;
            _playerInput.CameraController.Down.performed += OnDown;
            _playerInput.CameraController.Down.canceled += OnDown;

            _playerInput.Enable();
        }

        private void OnDisable()
        {
            _playerInput.CameraController.Move.started -= OnLeftStickInput;
            _playerInput.CameraController.Move.performed -= OnLeftStickInput;
            _playerInput.CameraController.Move.canceled -= OnLeftStickInput;
        
            _playerInput.CameraController.RightStick.started -= OnRightInput;
            _playerInput.CameraController.RightStick.performed -= OnRightInput;
            _playerInput.CameraController.RightStick.canceled -= OnRightInput;
        
            _playerInput.CameraController.Boost.started -= OnBoost;
            _playerInput.CameraController.Boost.performed -= OnBoost;
            _playerInput.CameraController.Boost.canceled -= OnBoost;
        
            _playerInput.CameraController.Up.started -= OnUp;
            _playerInput.CameraController.Up.performed -= OnUp;
            _playerInput.CameraController.Up.canceled -= OnUp;
        
            _playerInput.CameraController.Down.started -= OnDown;
            _playerInput.CameraController.Down.performed -= OnDown;
            _playerInput.CameraController.Down.canceled -= OnDown;

            _playerInput.Disable();
        }

        private void OnLeftStickInput(InputAction.CallbackContext ctx)
        {
            _leftStickInput = ctx.ReadValue<Vector2>();
        }
    
        private void OnRightInput(InputAction.CallbackContext ctx)
        {
            _rightStickInput = ctx.ReadValue<Vector2>();
        }

        private void OnBoost(InputAction.CallbackContext ctx)
        {
            BoostPressed = ctx.ReadValueAsButton();
        }
    
        private void OnUp(InputAction.CallbackContext ctx)
        {
            UpPressed = ctx.ReadValueAsButton();
        }
    
        private void OnDown(InputAction.CallbackContext ctx)
        {
            DownPressed = ctx.ReadValueAsButton();
        }
    
    
    }
}
