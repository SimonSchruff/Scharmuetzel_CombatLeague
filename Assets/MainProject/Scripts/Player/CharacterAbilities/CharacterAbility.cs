using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Assertions;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class CharacterAbility : NetworkBehaviour
    {
       // [Tooltip("the sound fx to play when the ability starts")]
       // public AudioClip AbilityStartAudioSfx;
        
       // [Tooltip("the sound fx to play while the ability is running")]
       // public AudioClip AbilityInProgressAudioSfx;
        
       // [Tooltip("the sound fx to play when the ability stops")]
       // public AudioClip AbilityStopAudioSfx;
        
        // [Tooltip("the feedbacks to play when the ability starts")]
        // MM Feedbacks class
        [Header("Prediction")]
        [Tooltip("if true, this ability performs on the local client without waiting for the servers response")]
        public bool AbilityPredicted = false;
        
        [Header("Permission")]
        [Tooltip("if true, this ability can perform as usual, if not, it'll be ignored. You can use this to unlock abilities over time for example")]
        public bool AbilityPermitted = true;
        [Tooltip("an array containing all the blocking movement states. If the Character is in one of these states and tries to trigger this ability, it won't be permitted. Useful to prevent this ability from being used while Idle or Swimming, for example.")]
        public CharacterStates.CharacterMovementStates[] BlockingMovementStates;
        [Tooltip("an array containing all the blocking condition states. If the Character is in one of these states and tries to trigger this ability, it won't be permitted. Useful to prevent this ability from being used while dead, for example.")]
        public CharacterStates.CharacterConditions[] BlockingConditionStates;
        public virtual bool AbilityAuthorized
        {
	        get
	        {
		        if (_character != null)
		        {
			        if ((BlockingMovementStates != null) && (BlockingMovementStates.Length > 0))
			        {
				        for (int i = 0; i < BlockingMovementStates.Length; i++)
				        {
					        if (BlockingMovementStates[i] == (_character.NetworkState.CurrentMovementState))
					        {
						        return false;
					        }    
				        }
			        }

			        if ((BlockingConditionStates != null) && (BlockingConditionStates.Length > 0))
			        {
				        for (int i = 0; i < BlockingConditionStates.Length; i++)
				        {
					        if (BlockingConditionStates[i] == (_character.NetworkState.CurrentConditionState))
					        {
						        return false;
					        }    
				        }
			        }
		        }
		        return AbilityPermitted;
	        }
        }
        
        [Space(10)]
        public AbilityCooldown Cooldown;

        /// whether or not this ability has been initialized
        public bool AbilityInitialized { get { return _abilityInitialized; } }
        
        protected TopDownController _controller;
        protected Character _character;
        protected PlayerMovement _playerMovement;
        protected GameObject _model;
        protected PlayerInputHandler _inputHandler;
        protected PlayerAbilityHandler _abilityHandler;
        protected PlayerFXHandler _fxHandler;
        protected Animator _animator = null;
        protected NetworkAnimator _netAnimator = null;
        
        // protected CharacterStates _state;
        protected StateMachine<CharacterStates.CharacterMovementStates> _movementStateMachine;
        protected StateMachine<CharacterStates.CharacterConditions> _conditionsStateMachine;

        protected bool _abilityInitialized = false;
        
        protected float left_stick_input_Y;
        protected float left_stick_input_X;
        // TODO: Add other inputs here and to reset method
        
        protected bool _startFeedbackIsPlaying = false;
        
        /// <summary>
        /// On awake we proceed to pre initializing our ability
        /// </summary>
        protected virtual void Awake()
        {
	       // PreInitialization();
        }

        /// <summary>
        /// On Start(), we call the ability's intialization
        /// </summary>
        public virtual void InitalizeAbility()
        {
	        Initialization();

	        if (IsLocalPlayer)
	        {
		        InitializeLocalPlayer();
	        }

	        if (IsServer)
	        {
				InitializeServer();   
	        }
	        
	        // subscribe to events
	        
	        // Success
	        _abilityInitialized = true;
        }

        public override void OnNetworkDespawn()
        {
			// unsubscribe to events
        }

        /// <summary>
        /// Gets and stores components for further use
        /// </summary>
        protected virtual void Initialization()
        {
	         
	         // Get character
	       	 _character = this.gameObject.GetComponent<Character>();
	         Assert.IsNotNull(_character);
	         
	         // Set up animation
	         BindAnimator();
	         
	         // other components
	         _controller = _character.LinkedPlayerController;
	         _inputHandler = _character.LinkedInputHandler;
	         _model = _character.CharacterModel;
	         
	         // Abilities
	         _abilityHandler = this.gameObject.GetComponent<PlayerAbilityHandler>(); 
	         _fxHandler = this.gameObject.GetComponent<PlayerFXHandler>(); 
	         _playerMovement = this.gameObject.GetComponent<PlayerMovement>(); 
	         
	         // Player state and state machines
	         _movementStateMachine = _character.MovementStateMachine;
	         _conditionsStateMachine = _character.ConditionStateMachine;
        }
        
        /// <summary>
        /// Gets and stores components for local client
        /// </summary>
        protected virtual void InitializeLocalPlayer()
        {
	        Assert.IsTrue(IsLocalPlayer);
	        
			
        }
        
        /// <summary>
        /// Gets and stores components for server only
        /// </summary>
        protected virtual void InitializeServer()
        {
	        Assert.IsTrue(IsServer);

        }

        #region INPUT
        /// <summary>
        /// Called at the very start of the ability's cycle, and intended to be overridden, looks for input and calls methods if conditions are met
        /// </summary>
        protected virtual void HandleInput(PlayerInputs inputs)
        {
	        
        }
        
        /// <summary>
        /// Resets all input for this ability. Can be overridden for ability specific directives
        /// </summary>
        public virtual void ResetInput()
        {
	        left_stick_input_X = 0f;
	        left_stick_input_Y = 0f;
        }
        #endregion
        
        /// <summary>
        /// The first of the 3 passes you can have in your ability. Think of it as EarlyUpdate() if it existed
        /// </summary>
        public virtual void EarlyProcessAbility(PlayerInputs inputs)
        {
	        HandleInput(inputs);
        }

        /// <summary>
        /// The second of the 3 passes you can have in your ability. Think of it as Update()
        /// </summary>
        public virtual void ProcessAbility()
        {
			
        }

        /// <summary>
        /// The last of the 3 passes you can have in your ability. Think of it as LateUpdate()
        /// </summary>
        public virtual void LateProcessAbility()
        {
			
        }
        
        #region ANIMATION
        /// <summary>
        /// Binds the animator from the character and initializes the animator parameters
        /// </summary>
        protected virtual void BindAnimator()
        {
	        if (_character._animator == null)
	        {
		        _character.AssignAnimator();
	        }

	        _animator = _character._animator;
	        _netAnimator = _character._netAnimator;

	        if (_animator != null)
	        {
		        InitializeAnimatorParameters();
	        }
        }
        
        /// <summary>
        /// Override this to send parameters to the character's animator. This is called once per cycle, by the Character class, after Early, normal and Late process().
        /// </summary>
        public virtual void UpdateAnimator()
        {
			
        }
        
        /// <summary>
        /// Adds required animator parameters to the animator parameters list if they exist
        /// </summary>
        protected virtual void InitializeAnimatorParameters()
        {

        }
        
        /// <summary>
        /// Registers a new animator parameter to the list
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="parameterType">Parameter type.</param>
        protected virtual void RegisterAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType, out int parameter)
        {
	        parameter = Animator.StringToHash(parameterName);

	        if (_animator == null) 
	        {
		        return;
	        }
	        
	        if (_character != null)
	        {
		        _character._animatorParameters.Add(parameter);	
	        }
        }
        #endregion
        
        /// <summary>
        /// Changes the status of the ability's permission
        /// </summary>
        /// <param name="abilityPermitted">If set to <c>true</c> ability permitted.</param>
        public virtual void PermitAbility(bool abilityPermitted)
        {
	        AbilityPermitted = abilityPermitted;
        }
        
        /// <summary>
        /// Override this to reset this ability's parameters. It'll be automatically called when the character gets killed, in anticipation for its respawn.
        /// </summary>
        public virtual void ResetAbility()
        {
			
        }

        /// <summary>
        /// Plays the ability start sound effect
        /// </summary>
        protected virtual void PlayAbilityStartSfx()
        {
	        
        }

        /// <summary>
        /// Override this to describe what should happen to this ability when the character respawns
        /// </summary>
        protected virtual void OnDeath()
        {
	        
        }
        
        /// <summary>
        /// Override this to describe what should happen to this ability when the character takes a hit
        /// </summary>
        protected virtual void OnHit()
        {

        }

    }
}