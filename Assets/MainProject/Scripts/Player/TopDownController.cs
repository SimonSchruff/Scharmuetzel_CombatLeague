using System;
using MainProject.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class TopDownController : NetworkBehaviour
    {
        [Header("Gravity")]
        [Tooltip("the current gravity to apply to our character (positive goes down, negative goes up, higher value, higher acceleration)")]
        public float Gravity = 40f;
        [Tooltip("whether or not the gravity is currently being applied to this character")]
        public bool GravityActive = true;
        
        [Header("Raycasts")]
        [Tooltip("the layer to consider as obstacles for team 01 (will prevent movement)")]
        public LayerMask Team01ObstaclesLayerMask;
        [Tooltip("the layer to consider as obstacles for team 02 (will prevent movement)")]
        public LayerMask Team02ObstaclesLayerMask;

        private LayerMask _obstacleLayerMask;

        [Tooltip("the current speed of the character")]
        [HideInInspector] public Vector3 Speed;
        [Tooltip("the current velocity in units/second")]
        [HideInInspector] public Vector3 Velocity;
        [Tooltip("the velocity of the character last frame")]
        [HideInInspector] public Vector3 VelocityLastFrame;
        [Tooltip("the current acceleration")]
        [HideInInspector] public Vector3 Acceleration;
        
        [Tooltip("the speed at which external forces get lerped to zero")]
        public float ImpactFalloff = 5f;
        
        [Tooltip("the current input sent to this character")]
        [HideInInspector] public Vector3 InputMoveDirection = Vector3.zero;
        [Tooltip("the current movement of the character")]
        [HideInInspector] public Vector3 CurrentMovement;
        [Tooltip("the direction the character is going in")]
        [HideInInspector] public Vector3 CurrentDirection;

        protected Transform _transform;
        protected Rigidbody _rigidBody;
        protected Collider _collider;
        protected Character _character;
        protected CharacterController _characterController;
        protected float _originalColliderHeight;
        protected Vector3 _originalColliderCenter;
        
        // velocity
        protected Vector3 _newVelocity;
        protected Vector3 _lastHorizontalVelocity;
        protected Vector3 _newHorizontalVelocity;
        protected Vector3 _motion;
        protected Vector3 _idealVelocity;
        protected Vector3 _idealDirection;
        protected Vector3 _horizontalVelocityDelta;
        protected float _stickyOffset = 0f;
        
        protected Vector3 _positionLastFrame;
        protected Vector3 _speedComputation;
        protected bool _groundedLastFrame;
        protected Vector3 _impact;		
        protected const float _smallValue = 0.0001f;
        
        // char movement
        protected float _deltaTime;
        protected CollisionFlags _collisionFlags;
        
        // move position
        protected RaycastHit _movePositionHit;
        protected Vector3 _capsulePoint1;
        protected Vector3 _capsulePoint2;
        protected Vector3 _movePositionDirection;
        protected float _movePositionDistance;


        private bool _isInitalized = false;

        /// <summary>
        /// On awake, we initialize our dependencies
        /// </summary>
        private void Awake()
        {
            _transform = this.transform;
            _characterController = this.gameObject.GetComponent<CharacterController>();
            _character = this.gameObject.GetComponent<Character>();
            _rigidBody = this.gameObject.GetComponent<Rigidbody>();
            _collider = this.gameObject.GetComponent<Collider>();
        }

        /// <summary>
        /// Initalize from character to set all relevant variables like teamId
        /// </summary>
        public void Initalize()
        {
            CurrentDirection = transform.forward;
            
            _originalColliderHeight = _characterController.height;
            _originalColliderCenter = _characterController.center;

            // Sets the layer mask depending on team
            _obstacleLayerMask = _character.Net_TeamID.Value == 1 ? Team01ObstaclesLayerMask : Team02ObstaclesLayerMask;

            _isInitalized = true;
        }

        protected virtual void  FixedUpdate()
        {
           // ApplyImpact();
            
           // ProcessUpdate();
        }

        /// <summary>
        /// On late update we apply any impact we have in store, and store our velocity for use next frame
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (_isInitalized == false) { return; }
            
            VelocityLastFrame = Velocity;
        }

        /// <summary>
        /// Computes the new velocity and moves the character
        /// </summary>
        protected virtual void ProcessUpdate()
        {
            _newVelocity = Velocity;
            _positionLastFrame = _transform.position;
            
            AddInput();
            ComputeVelocityDelta();
            MoveCharacterController();
            ComputeNewVelocity();
            ComputeSpeed();
            DetermineDirection();
            
            // CurrentMovement = Vector3.zero;
        }
        

        /// <summary>
        /// Determines the new velocity based on the slope we're on and the input 
        /// </summary>
        protected virtual void AddInput()
        {
            _idealVelocity = CurrentMovement;
            
            Vector3 sideways = Vector3.Cross(Vector3.up, _idealVelocity);
            _idealVelocity = Vector3.Cross(sideways, Vector3.up).normalized * _idealVelocity.magnitude;
            
            _newVelocity = _idealVelocity;
            _newVelocity.y = Mathf.Min(_newVelocity.y, 0);
        }
        
        
        /// <summary>
        /// Computes the motion vector to apply to the character controller 
        /// </summary>
        protected virtual void ComputeVelocityDelta()
        {
            _motion = _newVelocity * _deltaTime;
            _horizontalVelocityDelta.x = _motion.x;
            _horizontalVelocityDelta.y = 0f;
            _horizontalVelocityDelta.z = _motion.z;
            _stickyOffset = Mathf.Max(_characterController.stepOffset, _horizontalVelocityDelta.magnitude);
            _motion -= _stickyOffset * Vector3.up;
        }
        
        /// <summary>
        /// Moves the character controller by the computed _motion and steps the physics scene
        /// </summary>
        protected virtual void MoveCharacterController()
        {
            _collisionFlags = _characterController.Move(_motion); // controller move
        }
        
        /// <summary>
        /// Determines the new Velocity value based on our position and our position last frame
        /// </summary>
        protected virtual void ComputeNewVelocity()
        {
            Velocity = _newVelocity;
            Acceleration = (Velocity - VelocityLastFrame) / _deltaTime;
        }
        
        /// <summary>
        /// Sets the character's current input direction and magnitude
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="dt"></param>
        public virtual void SetMovement(Vector3 movement, float dt)
        {
            if (_isInitalized == false) { return; }

            CurrentMovement = movement;
            _deltaTime = dt;

            Vector3 directionVector = movement;
            if (directionVector != Vector3.zero) {
                float directionLength = directionVector.magnitude;
                directionVector = directionVector / directionLength;
                directionLength = Mathf.Min(1, directionLength);
                directionLength = directionLength * directionLength;
                directionVector = directionVector * directionLength;
            }
            
            InputMoveDirection = transform.rotation * directionVector;
            
            ProcessUpdate();
        }

        /// <summary>
        /// Computes the speed
        /// </summary>
        protected virtual void ComputeSpeed ()
        {
            if (_deltaTime != 0f)
            {
                Speed = (this.transform.position - _positionLastFrame) / _deltaTime;
            }			
            // we round the speed to 2 decimals
            Speed.x = Mathf.Round(Speed.x * 100f) / 100f;
            Speed.y = Mathf.Round(Speed.y * 100f) / 100f;
            Speed.z = Mathf.Round(Speed.z * 100f) / 100f;
            _positionLastFrame = this.transform.position;
        }
        
        /// <summary>
        /// Determines the direction based on the current movement
        /// </summary>
        protected virtual void DetermineDirection()
        {
            if (CurrentMovement.magnitude > 0f)
            {
                CurrentDirection = CurrentMovement.normalized;
            }
        }
        
        /// <summary>
        /// Applies the stored impact to the character
        /// </summary>
        protected virtual void ApplyImpact()
        {
            if (_impact.magnitude > 0.2f)
            {
                _characterController.Move(_impact * Time.fixedDeltaTime);
            }
            _impact = Vector3.Lerp(_impact, Vector3.zero, ImpactFalloff * Time.fixedDeltaTime);
        }
        
        /// <summary>
        /// Moves this character to the specified position while trying to avoid obstacles
        /// </summary>
        /// <param name="newPosition"></param>
        public virtual void MovePosition(Vector3 newPosition)
        {
            
            _movePositionDirection = (newPosition - this.transform.position);
            _movePositionDistance = Vector3.Distance(this.transform.position, newPosition) ;

            _capsulePoint1 =    this.transform.position 
                                + _characterController.center 
                                - (Vector3.up * _characterController.height / 2f) 
                                + Vector3.up * _characterController.skinWidth 
                                + Vector3.up * _characterController.radius;
            _capsulePoint2 =    this.transform.position
                                + _characterController.center
                                + (Vector3.up * _characterController.height / 2f)
                                - Vector3.up * _characterController.skinWidth
                                - Vector3.up * _characterController.radius;

            if (!Physics.CapsuleCast(_capsulePoint1, _capsulePoint2, _characterController.radius, _movePositionDirection, out _movePositionHit, _movePositionDistance, _obstacleLayerMask))
            {
                this.transform.position = newPosition;
            }
        }
        
        /// <summary>
        /// Resets all values for this controller
        /// </summary>
        public virtual void Reset()
        {
            _impact = Vector3.zero;
            GravityActive = true;
            Speed = Vector3.zero;
            Velocity = Vector3.zero;
            VelocityLastFrame = Vector3.zero;
            Acceleration = Vector3.zero;
            CurrentMovement = Vector3.zero;
            CurrentDirection = Vector3.zero;
        }
    }
}