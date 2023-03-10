using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player;
using UnityEngine;

public class Spectator : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 10f; 
    [SerializeField] private float BoostSpeed = 10f; 
    [SerializeField] private float RotationSpeed = 10f;

    private SpectatorInputHandler _inputHandler;

    private Vector2 _leftStickInput;
    private Vector2 _rightStickInput;
    private bool _boostInput;
    private bool _upInput;
    private bool _downInput;
    
    void Awake()
    {
        _inputHandler = GetComponent<SpectatorInputHandler>();
    }

    void Update()
    {
        _leftStickInput = _inputHandler.LeftStickInput;
        _rightStickInput = _inputHandler.RightStickInput;
        _boostInput = _inputHandler.BoostPressed;
        _upInput = _inputHandler.UpPressed;
        _downInput = _inputHandler.DownPressed;
        
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 forward = Vector3.forward * _leftStickInput.y;
        Vector3 right = Vector3.right * _leftStickInput.x;
        Vector3 up = _upInput ?  Vector3.up : Vector3.zero;
        Vector3 down = _downInput ?  Vector3.down : Vector3.zero;
        
        float speed = _boostInput ? BoostSpeed : MoveSpeed;

        Vector3 newPos = transform.position + (forward + right + up + down);
        Vector3 lerpedMove = Vector3.Lerp(transform.position, newPos, Time.deltaTime * speed);

        transform.position = lerpedMove;
    }
    
    private void HandleRotation()
    {
        float yaw = RotationSpeed * _rightStickInput.x;
        float pitch = RotationSpeed * _rightStickInput.y;

        Quaternion currRot = transform.rotation;
        Quaternion newRot = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(pitch, yaw, 0f));
        
        transform.rotation = Quaternion.Lerp(currRot, newRot, Time.deltaTime);
    }
}