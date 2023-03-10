using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures.PlayerData;
using UnityEngine;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    /// <summary>
    /// Handles movement and rotation of the player;
    /// </summary>
    public class PlayerMovement : CharacterAbility
    {
        [Tooltip("whether or not movement input is authorized at that time")]
        public bool InputAuthorized = true;
        
        [Header("Movement")]
        [Tooltip("the current reference movement speed")]
        public float MovementSpeed = 10f;
        /// if this is true, movement will be forbidden
        public bool MovementForbidden { get; set; }
        [Tooltip("the acceleration to apply to the current speed / 0f : no acceleration, instant full speed")]
        public float Acceleration = 10f;
        [Tooltip("the deceleration to apply to the current speed / 0f : no deceleration, instant stop")]
        public float Deceleration = 10f;
        [Tooltip("whether or not to interpolate movement speed")]
        public bool InterpolateMovementSpeed = false;
        [Tooltip("the speed threshold after which the character is not considered idle anymore")]
        public float IdleThreshold = 0.05f;
        public float MovementSpeedMaxMultiplier { get; set; } = float.MaxValue;
        private float _movementSpeedMultiplier;
        /// the multiplier to apply to the horizontal movement
        public float MovementSpeedMultiplier {
            get => Mathf.Min(_movementSpeedMultiplier, MovementSpeedMaxMultiplier);
            set => _movementSpeedMultiplier = value;
        }
        
        /// the multiplier to apply to the horizontal movement, applied by contextual elements (movement zones, etc)
        public Stack<float> ContextSpeedStack = new Stack<float>();
        public float ContextSpeedMultiplier => ContextSpeedStack.Count > 0 ? ContextSpeedStack.Peek() : 1f;
        
        [Header("Rotation")]
        [Tooltip("the speed at which to rotate towards direction (applies only if IsRotInstant is false)")]
        public float ROTATION_SPEED = 10f;
        [Tooltip("If true the character rotates instantly in the desired direction, without smoothing;")]
        public bool IsRotationInstant;
        [Tooltip("the direction of the model")]
        [HideInInspector] public Vector3 ModelDirection;
        [Tooltip("the direction of the model in angle values")]
        [HideInInspector] public Vector3 ModelAngles;
        
        // Private Rotation Vars
        private GameObject _rotatingModel;
        protected Vector3 _currentDirection;
        protected Quaternion _tmpRotation;
        protected Quaternion _newMovementQuaternion;
        
        // Private Move Vars
        protected float _movementSpeed;
        protected float _horizontalMovement;
        protected float _verticalMovement;
        protected Vector3 _movementVector;
        protected PlayerInputs _currentInput;
        protected Vector2 _normalizedInput;
        protected Vector2 _lerpedInput = Vector2.zero;
        protected float _acceleration = 0f;
        
        // Animation 
        protected int _speedAnimationParameter;

        protected override void Initialization()
        {
            base.Initialization();

            MovementSpeedMultiplier = 1f;
            MovementForbidden = false;

            _rotatingModel = this.gameObject;

            // _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            
        }

        // Gets called from early process ability and executes before processing ability
        protected override void HandleInput(PlayerInputs inputs)
        {
            _currentInput = inputs;
            
            if (InputAuthorized) {
                _horizontalMovement = inputs.left_stick_input.x;
                _verticalMovement = inputs.left_stick_input.y;
            }
            else {
                _horizontalMovement = 0f;
                _verticalMovement = 0f;
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (!IsServer && !IsLocalPlayer) {
                return;
            }
            
            if (!AbilityAuthorized) {
                return;
            }
            
            HandleFrozen();
            HandleMovement();
            RotateToFaceMovementDirection();
        }
        
        #region MOVEMENT
        /// <summary>
		/// Called at Update(), handles horizontal movement
		/// </summary>
		protected virtual void HandleMovement()
		{

			// if movement is prevented, or if the character is dead/frozen/can't move, we exit and do nothing
			if ( !AbilityAuthorized || (_conditionsStateMachine.CurrentState != CharacterStates.CharacterConditions.Normal) )
			{
				return;				
			}
			
			if (MovementForbidden)
			{
				_horizontalMovement = 0f;
				_verticalMovement = 0f;
			}

            if ((_controller.CurrentMovement.magnitude > IdleThreshold) && ( _movementStateMachine.CurrentState == CharacterStates.CharacterMovementStates.Idle))
			{				
				_movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Running);	
				//PlayAbilityStartSfx();	
				//PlayAbilityUsedSfx();
				//PlayAbilityStartFeedbacks();
			}
            
			// if we're running and not moving anymore, we go back to the Idle state
			if ((_controller.CurrentMovement.magnitude <= IdleThreshold) && (_movementStateMachine.CurrentState == CharacterStates.CharacterMovementStates.Running ||_movementStateMachine.CurrentState == CharacterStates.CharacterMovementStates.None))
			{
				_movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
				// PlayAbilityStopSfx();
				// PlayAbilityStopFeedbacks();
			}
            
			SetMovement();
        }
        
        /// <summary>
        /// Moves the controller
        /// </summary>
        protected virtual void SetMovement()
        {
            _movementVector = Vector3.zero;
            _currentInput.left_stick_input = Vector2.zero;

            _currentInput.left_stick_input.x = _horizontalMovement;
            _currentInput.left_stick_input.y = _verticalMovement;

            _normalizedInput = _currentInput.left_stick_input.normalized;
            
            float interpolationSpeed = 1f;
            
            if ((Acceleration == 0) || (Deceleration == 0))
            {
                _lerpedInput = _currentInput.left_stick_input;
            }
            else
            {
                if (_normalizedInput.magnitude == 0)
                {
                    _acceleration = Mathf.Lerp(_acceleration, 0f, Deceleration * Time.fixedDeltaTime);
                    _lerpedInput = Vector2.Lerp(_lerpedInput, _lerpedInput * _acceleration, Time.fixedDeltaTime * Deceleration);
                    interpolationSpeed = Deceleration;
                }
                else
                {
                    _acceleration = Mathf.Lerp(_acceleration, 1f, Acceleration * Time.fixedDeltaTime);
                    _lerpedInput = Vector2.ClampMagnitude (_currentInput.left_stick_input, _acceleration);
                    interpolationSpeed = Acceleration;
                }
            }		
			
            _movementVector.x = _lerpedInput.x;
            _movementVector.y = 0f;
            _movementVector.z = _lerpedInput.y;

            if (InterpolateMovementSpeed)
            {
                _movementSpeed = Mathf.Lerp(_movementSpeed, MovementSpeed * ContextSpeedMultiplier * MovementSpeedMultiplier, interpolationSpeed * Time.fixedDeltaTime);
            }
            else
            {
                _movementSpeed = MovementSpeed * MovementSpeedMultiplier * ContextSpeedMultiplier;
            }
            
            _movementVector *= _movementSpeed;

            if (_movementVector.magnitude > MovementSpeed * ContextSpeedMultiplier * MovementSpeedMultiplier)
            {
                _movementVector = Vector3.ClampMagnitude(_movementVector, MovementSpeed);
            }

            if ((_currentInput.left_stick_input.magnitude <= IdleThreshold) && (_controller.CurrentMovement.magnitude < IdleThreshold))
            {
                _movementVector = Vector3.zero;
            }
            
            _controller.SetMovement(_movementVector, Time.fixedDeltaTime);
        }
        #endregion
        
        #region MOVEMENT_MODIFIERS
        /// <summary>
        /// Applies a movement multiplier for the specified duration
        /// </summary>
        /// <param name="movementMultiplier"></param>
        /// <param name="duration"></param>
        public virtual void ApplyMovementMultiplier(float movementMultiplier, float duration)
        {
            StartCoroutine(ApplyMovementMultiplierCo(movementMultiplier, duration));
        }
		
        /// <summary>
        /// A coroutine used to apply a movement multiplier for a certain duration only
        /// </summary>
        /// <param name="movementMultiplier"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        protected virtual IEnumerator ApplyMovementMultiplierCo(float movementMultiplier, float duration)
        {
            SetContextSpeedMultiplier(movementMultiplier);
            yield return new WaitForSeconds(duration);
            ResetContextSpeedMultiplier();
        }
        /// <summary>
        /// Stacks a new context speed multiplier
        /// </summary>
        /// <param name="newMovementSpeedMultiplier"></param>
        public virtual void SetContextSpeedMultiplier(float newMovementSpeedMultiplier)
        {
            ContextSpeedStack.Push(newMovementSpeedMultiplier);
        }

        /// <summary>
        /// Revers the context speed multiplier to its previous value
        /// </summary>
        public virtual void ResetContextSpeedMultiplier()
        {
            ContextSpeedStack.Pop();
        }
        
        /// <summary>
        /// Describes what happens when the character is in the frozen state
        /// </summary>
        protected virtual void HandleFrozen()
        {
            
            if (_conditionsStateMachine.CurrentState == CharacterStates.CharacterConditions.Frozen || _conditionsStateMachine.CurrentState == CharacterStates.CharacterConditions.Stunned)
            {
                Debug.Log($"Player {OwnerClientId} is frozen!");
                _horizontalMovement = 0f;
                _verticalMovement = 0f;
                SetMovement();
            }
            
        }
        #endregion

        #region ROTATION
        /// <summary>
        /// Rotates the player model to face the current direction
        /// </summary>
        protected virtual void RotateToFaceMovementDirection()
        {
            _currentDirection =  _controller.CurrentDirection;
            
            // Rotate model instantly to current direction
            if (IsRotationInstant == true && _currentDirection != Vector3.zero)
            {
                _newMovementQuaternion = Quaternion.LookRotation(_currentDirection);
            }
            
            // Smoothly rotate model towards current direction with RotationSpeed
            if (IsRotationInstant == false && _currentDirection != Vector3.zero)
            {
                _tmpRotation = Quaternion.LookRotation(_currentDirection);
                _newMovementQuaternion = Quaternion.Slerp(_rotatingModel.transform.rotation, _tmpRotation, Time.fixedDeltaTime * ROTATION_SPEED);
            }
            
            ModelDirection = _rotatingModel.transform.forward.normalized;
            ModelAngles = _rotatingModel.transform.eulerAngles;
            
            _rotatingModel.transform.rotation = _newMovementQuaternion;
        }
        #endregion

        #region ANIMATION
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter("MoveSpeed", AnimatorControllerParameterType.Float, out _speedAnimationParameter);
        }

        public override void UpdateAnimator()
        {
            if (!IsServer) {
                return;
            }
            
            // Only update animator when its necessary to save bandwidth
            if (_controller.CurrentMovement.sqrMagnitude > 0)
            {
                _animator.SetFloat(_speedAnimationParameter,Mathf.Abs(_controller.Speed.magnitude));
            }
        }
        #endregion
    }
}