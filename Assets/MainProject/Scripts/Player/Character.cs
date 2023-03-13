using System;
using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player.CharacterAbilities;
using MainProject.Scripts.Player.PlayerUI;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Assertions;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MainProject.Scripts.Player
{
    /// <summary>
    /// This class will pilot the TopDownController component of your character.
    /// This is where you'll implement all of your character's game rules, like jump, dash, shoot, stuff like that.
    /// </summary>
    public class Character : NetworkBehaviour
    {
        public NetworkVariable<NetworkString> Net_PlayerName = new NetworkVariable<NetworkString>();
        public NetworkVariable<int> Net_TeamID = new NetworkVariable<int>();

        [Header("Bindings")]
        [Tooltip("the 'model' (can be any GameObject) used to manipulate the character. Ideally it's separated (and nested) from the collider/TopDown controller/abilities, to avoid messing with collisions.")]
        public GameObject CharacterModel;
        [Tooltip("The health bar associated with this player")]
        public GameObject HealthBarPrefab;
        private HealthBar _healthBar;
        
        [Space(10)]
        [Tooltip("The player controller associated with this character")]
        public TopDownController LinkedPlayerController;
        [Tooltip("The character controller associated with this character")]
        public CharacterController LinkedCharacterController;
        [Tooltip("The health class associated with this character")]
        public Health LinkedHealth;
        [Tooltip("The server authoritative state of the player")]
        public PlayerNetworkState NetworkState;
        [Tooltip("The player HUD handler associated with this character")]
        public PlayerHUDManager LinkedPlayerHUDManager;
        public PlayerAbilityHandler LinkedAbilityHandler;
        public PlayerCameraHandler LinkedCameraHandler;
        public PlayerFXHandler LinkedFXHandler;
        public PlayerSoundFXHandler LinkedSoundFXHandler;
        
        [Space(10)]

        // State Machines
        public StateMachine<CharacterStates.CharacterMovementStates> MovementStateMachine;
        public StateMachine<CharacterStates.CharacterConditions> ConditionStateMachine;
        
        // Abilities
        private PlayerMovement _playerMovement;
        private AreaSpikeAttack _areaSpikeAbility;
        protected CharacterAbility[] _characterAbilities;
        
        [Header("Animators")]
        public Animator CharacterAnimator;
        public NetworkAnimator CharacterNetAnimator;
        /// the animators associated to this character
        public Animator _animator { get; protected set; }
        public NetworkAnimator _netAnimator { get; protected set; }
        /// a list of animator parameters
        public HashSet<int> _animatorParameters { get; set; }
        protected bool _animatorInitialized = false;

        [Space(20)] [Header("Debug Area")] 
        [Tooltip("If true, confirmed server position is displayed on local client")]
        public bool IsServerDisplayPlayerEnabled;
        public GameObject ServerDisplayPlayer;
        [Space(10)] 
        public bool IsSmoothingEnabled = true;
        [Tooltip("If false client will not try to reconcile latest state msg form server")]
        public bool IsCorrectionEnabled = true;
        [Tooltip("If true, client will always rollback to the tick of the received state message and simulate forward")]
        public bool IsDebugReconciliationEnabled;
        private Transform s_display_transform;

        /// ==== INPUT =====
        public PlayerInputHandler LinkedInputHandler { get; protected set; }
        private List<PlayerInputs> c_redundant_inputs = new List<PlayerInputs>();

        // ===== CLIENT =====
        private uint c_tick_number = 1; // Start ticking at 1, so 'x = tick - input.length' is not negative
        private uint c_last_received_state_tick = 0;
        private uint c_wait_for_server_tick;
        private Vector3 c_pos_error = Vector3.zero;
        private Quaternion c_rot_error = Quaternion.identity;

        private const int CLIENT_BUFFER_SIZE = 1024;
        private ClientState[] c_state_buffer;
        private PlayerInputs[] c_input_buffer;
        
        private float c_time_elapsed;

        private Queue<StateMessage> c_movement_state_msg_queue;
        private bool _isLocalClientInitalized = false;
        private bool _isRemoteClientInitalized = false;


        // ===== SERVER =====
        private Queue<InputMessage> s_input_msg_queue;
        private uint s_last_processed_tick = 0;
        private bool _isServerInitalized = false;
        
        // STATIC EVENTS
        public static event Action<Character> OnCharacterSpawned;  
        public static event Action<Character> OnCharacterDespawned; 
        

        /// <summary>
        /// Initializes this instance of the character
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // Get components
            LinkedPlayerController = this.gameObject.GetComponent<TopDownController>();
            LinkedCharacterController = this.gameObject.GetComponent<CharacterController>();
            LinkedAbilityHandler = this.gameObject.GetComponent<PlayerAbilityHandler>();
            LinkedHealth = this.gameObject.GetComponent<Health>();
            LinkedFXHandler = this.gameObject.GetComponent<PlayerFXHandler>();
            LinkedSoundFXHandler = this.gameObject.GetComponent<PlayerSoundFXHandler>();
            _netAnimator = GetComponent<NetworkAnimator>();
            _playerMovement = GetComponent<PlayerMovement>();
            _areaSpikeAbility = GetComponent<AreaSpikeAttack>();
            
            // Character state
            NetworkState = GetComponent<PlayerNetworkState>();
            MovementStateMachine = new StateMachine<CharacterStates.CharacterMovementStates>(this.gameObject , false);
            ConditionStateMachine = new StateMachine<CharacterStates.CharacterConditions>(this.gameObject , false);

            // Set up client buffers and queue
            c_state_buffer = new ClientState[CLIENT_BUFFER_SIZE];
            c_input_buffer = new PlayerInputs[CLIENT_BUFFER_SIZE];
            c_movement_state_msg_queue = new Queue<StateMessage>();
            
            // Find and initalize abilities
            CacheAbilitiesAtInit();
            
            LinkedHealth.OnDeath += OnDeath;
            LinkedHealth.OnRespawn += OnRespawn;
            LinkedHealth.OnDamageTaken += OnHit;
            
            NetworkManager.NetworkTickSystem.Tick += TickPlayer;
            
            GameManager.Instance.OnTeamWon += OnGameOver;

            StartCoroutine(InitTimeDependVars());
        }

        public override void OnNetworkDespawn()
        {
            LinkedHealth.OnDeath -= OnDeath;
            LinkedHealth.OnRespawn -= OnRespawn;
            NetworkManager.NetworkTickSystem.Tick -= TickPlayer;
            OnCharacterDespawned?.Invoke(this);
            GameManager.Instance.OnTeamWon -= OnGameOver;
        }

        private IEnumerator InitTimeDependVars()
        {
            yield return new WaitForSeconds(1f);
            
            var playerModelMeshRenderer = this.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            var weapon = this.GetComponentInChildren<PlayerWeapon>();
            
            if (this.Net_TeamID.Value == 1) {
                playerModelMeshRenderer.material = PlayerManager.Instance.PlayerTeam01Material;
                weapon.SetMaterial(PlayerManager.Instance.PlayerTeam01Material);
            }

            if (this.Net_TeamID.Value == 2) {
                playerModelMeshRenderer.material = PlayerManager.Instance.PlayerTeam02Material;
                weapon.SetMaterial(PlayerManager.Instance.PlayerTeam02Material);
            }
            
            this.CharacterModel.SetActive(false);

            OnCharacterSpawned?.Invoke(this);
            PlayerHUDManager.Instance.SetPlayerNameOnHealthBar(this);
            InitalizeAbilities();
            LinkedAbilityHandler.Init();
            
            if (IsLocalPlayer) {
                InitalizeLocalClient();
            }
            else {
                InitalizeRemoteClient();
            }

            if (IsServer) {
                InitializeServer();
            }
        }

        /// <summary>
        /// Sets up all components and relevant variables for the local player;
        /// Initalizes player controller, playerHUD
        /// </summary>
        public void InitalizeLocalClient()
        {
            LinkedInputHandler = this.gameObject.GetComponent<PlayerInputHandler>();
            LinkedAbilityHandler = this.gameObject.GetComponent<PlayerAbilityHandler>();
            LinkedCameraHandler = this.gameObject.GetComponent<PlayerCameraHandler>();
            LinkedPlayerController = this.gameObject.GetComponent<TopDownController>();
            this.LinkedInputHandler.enabled = false;

            if (Net_TeamID.Value == 1) {
                gameObject.layer = 11; // Team01 Layer
            }
            else {
                gameObject.layer = 12; // Team02 Layer
            }
            
            LinkedPlayerController.Initalize();
            LinkedCameraHandler.SetUpFollowPlayerCamera();
            PlayerHUDManager.Instance.InitalizeLocalPlayer();

            StartCoroutine(WaitToEnableMesh(3f));
            
            _isLocalClientInitalized = true;
        }
        
        public void InitalizeRemoteClient()
        {
            StartCoroutine(WaitToEnableMesh(3f));

            _isRemoteClientInitalized = true;
        }

        private IEnumerator WaitToEnableMesh(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (IsLocalPlayer) {
                this.LinkedInputHandler.enabled = true;
            }
            
            this.CharacterModel.SetActive(true);
            this.LinkedFXHandler.PlaySpawnFX(Net_TeamID.Value);
            this.LinkedSoundFXHandler.Play("Spawn");
        }

        

        public virtual void InitializeServer()
        {
            Assert.IsTrue(IsServer);

            LinkedPlayerController.Initalize();

            if (Net_TeamID.Value == 1) {
                gameObject.layer = 11; // Team01 Layer
            }
            else {
                gameObject.layer = 12; // Team02 Layer
            }

            MovementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            ConditionStateMachine.ChangeState(CharacterStates.CharacterConditions.Normal);
            
            s_input_msg_queue = new Queue<InputMessage>();
            
            _isServerInitalized = true;
        }

        protected virtual void TickPlayer()
        {
            // Only tick if the character has been initalized;
            if (!_isLocalClientInitalized && !_isServerInitalized && !_isRemoteClientInitalized) {
                return;
            }
            
            if (IsLocalPlayer)
            {
                HandleInputsLocalClient();
                
                AimAbilities();
                
                // If host we do not need to predict, since input reaches server (virtually) instantly 
                if (!IsServer) {
                    PredictMovementOnLocalClient();
                }
            }
            
            // Step server player with input, and send out state message
            if (IsServer) 
            {
                StepServerPlayer();
            }
            
            // Get Most recent state msg from server
            var stateMsg = DequeueMostRecentStateMsg();
            
            // CLIENT
            if(!IsServer)
            {
                // Movement
                if (NetworkState.CurrentMovementState == CharacterStates.CharacterMovementStates.Running || NetworkState.CurrentMovementState == CharacterStates.CharacterMovementStates.Idle || NetworkState.CurrentMovementState == CharacterStates.CharacterMovementStates.Dashing) 
                {
                    // Local Client
                    if (IsLocalPlayer)
                    {
                        ReconcileLocalClient(stateMsg);
                    }
                    
                    // Remote Client
                    if(!IsLocalPlayer) {
                        StepRemoteClientMovement(stateMsg);
                    }
                }
            }
            
            StepSmoothedPosRot();
            
            c_tick_number++;
        }

        #region INPUT
        private void HandleInputsLocalClient()
        {
            Assert.IsTrue(IsLocalPlayer);
            
            var dt = Time.fixedDeltaTime;

            // Gather inputs of current frame and check if we need to send them to server
            bool hasRelevantInput = GetCurrentInputs(out PlayerInputs currentPlayerInputs);

            // Add current input to redundant input list
            c_redundant_inputs.Add(currentPlayerInputs);

            // Limit list length to the last acknowledged state by server or 64 to prevent maxSendSize Error; Server does not use last received state tick, so prevent buffer overflow by setting count directly;
            var redundantInputCount = Math.Clamp(IsServer ? 16 : c_tick_number - c_last_received_state_tick, 1, 16);
            while (c_redundant_inputs.Count > redundantInputCount) {
                c_redundant_inputs.RemoveAt(0);
            }

            // Create InputMsg for current tick
            InputMessage inputMsg = new InputMessage {
                client_tick_number = c_tick_number,
                delta_time = dt,
                player_inputs = c_redundant_inputs.ToArray(),
            };
            
            // Record input in local input buffer
            var bufferIndex = c_tick_number % CLIENT_BUFFER_SIZE;
            c_input_buffer[bufferIndex] = currentPlayerInputs;

            // Only send input every second server tick
           // if (c_tick_number % 2 != 0)
            {
                // Clear buttons
                LinkedInputHandler.ClearButtonInput();
                // Send input to server
                EnqueueInputMsgServerRpc(inputMsg);
            }
        }

        private bool GetCurrentInputs( out PlayerInputs inputs)
        {
            // Gather all inputs from input handler
            PlayerInputs currentPlayerInputs = new PlayerInputs() {
                left_stick_input = LinkedInputHandler.LeftStickInput,
                basic_attack_input = LinkedInputHandler.BasicAttackKeyState,
                dash_input = LinkedInputHandler.DashKeyState,
                heal_input = LinkedInputHandler.HealKeyState,
                cast_01_input = LinkedInputHandler.Cast01KeyState,
                cast_02_input = LinkedInputHandler.Cast02KeyState,
                cast_03_input = LinkedInputHandler.Cast03KeyState,
                cast_04_input = LinkedInputHandler.Cast04KeyState,
            };
            
            inputs = currentPlayerInputs;

            if (currentPlayerInputs.left_stick_input.sqrMagnitude != 0) {
                return true;
            }

            if (currentPlayerInputs.basic_attack_input != KeyState.off) {
                return true;
            }
            
            if (currentPlayerInputs.dash_input != KeyState.off) {
                return true;
            }
            
            // If code reaches this point there is no input to send
            return false;
        }
        
        /// <summary>
        /// Predict stopping movement if we send ability input to the server and if local cooldown prediction is ready; 
        /// </summary>
        /// <returns>True: Client is allowed to move; False: Client should stop movement until server response; </returns>
        private bool PredictClientAllowedToMove(PlayerInputs inputs)
        {
            if (inputs.basic_attack_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.BasicAttack)) {
                    return false;
                }
            }
            
            if (inputs.cast_01_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.BasicRanged)) {
                    return false;
                }
            }
            
            if (inputs.cast_02_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.GroundSpikes)) {
                    return false;
                }
            }
            
            if (inputs.cast_03_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.SpinAttack)) {
                    return false;
                }
            }
            
            if (inputs.cast_04_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.DragonPunch)) {
                    return false;
                }
            }
            
            if (inputs.dash_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.Dash)) {
                    return false;
                }
            }
            
            if (inputs.heal_input != KeyState.off) {
                if (LinkedAbilityHandler.CheckIfAbilityIsReady(AbilityTypes.HealPotion)) {
                    return false;
                }
            }

            // If code reaches this point client is not sending any ability input and we can move
            return true;
        }

        /// <summary>
        /// Resets the input for all abilities
        /// </summary>
        public virtual void ResetInput()
        {
            if (_characterAbilities == null)
            {
                return;
            }
            foreach (CharacterAbility ability in _characterAbilities)
            {
               ability.ResetInput();
            }
        }
        #endregion

        #region CLIENT

        private StateMessage DequeueMostRecentStateMsg()
        {
            if (c_movement_state_msg_queue.Count < 1) {
                return null;
            }

            // Get most recent stateMsg
            var stateMsg = new StateMessage();
            while (c_movement_state_msg_queue.Count > 0) {
                stateMsg = c_movement_state_msg_queue.Dequeue();
            }

            c_last_received_state_tick = stateMsg.tick_number;

            return stateMsg;
        }

        private void AimAbilities()
        {
            // Do nothing if states are not allowing for ability to be cast
            if (this.NetworkState.CurrentMovementState == CharacterStates.CharacterMovementStates.Casting ||
                this.NetworkState.CurrentMovementState == CharacterStates.CharacterMovementStates.Dashing) {
                return;
            }
            
            if (this.NetworkState.CurrentConditionState != CharacterStates.CharacterConditions.Normal) {
                return;
            }
            
            // Get current input
            var bufferIndex = c_tick_number % CLIENT_BUFFER_SIZE;
            var inputs = c_input_buffer[bufferIndex];
            
            // Activate aim display
            LinkedAbilityHandler.HandleAimForAbilities(inputs);
        }
        
        private void PredictMovementOnLocalClient()
        {
            Assert.IsTrue(IsLocalPlayer && !IsServer);

            // Dont predict movement if we are waiting for server confirm that we are allowed to move
            if (c_last_received_state_tick < c_wait_for_server_tick) {
                return;
            }

            // Get current input
            var bufferIndex = c_tick_number % CLIENT_BUFFER_SIZE;
            var inputs = c_input_buffer[bufferIndex];

            // If we send ability input we predict and decide to not move until server response
            bool allowedToMove = PredictClientAllowedToMove(inputs);
            if (!allowedToMove) {
                c_wait_for_server_tick = c_tick_number;
                return;
            }
            
            _playerMovement.EarlyProcessAbility(inputs);
            _playerMovement.ProcessAbility();

            PhysicsManager.Instance.SyncTransforms();

            // Save resulting state
            c_state_buffer[bufferIndex] = new ClientState {
                position = transform.position,
                rotation = transform.rotation,
                condition_state = NetworkState.CurrentConditionState,
                movement_state = NetworkState.CurrentMovementState,
            };
        }

        /// <summary>
        /// Compare and Reset state of local client;
        /// If reconciliation is needed simulate forward and calculate smoothed pos/rot
        /// </summary>
        private void ReconcileLocalClient(StateMessage stateMsg)
        {
            Assert.IsTrue(IsLocalPlayer);

            if (stateMsg == null) {
                return;
            }

            // Get buffer index for comparision in state buffer
            var rewind_tick_number = stateMsg.tick_number;
            var bufferIndex = rewind_tick_number % CLIENT_BUFFER_SIZE;
            
            // Calculate Rotation or Position Error between client and server pos/rot
            var posError = c_state_buffer[bufferIndex].position - stateMsg.position;
            var rotError = 1.0f - Quaternion.Dot(stateMsg.rotation, c_state_buffer[bufferIndex].rotation);
            bool conditionStateError = c_state_buffer[bufferIndex].condition_state == stateMsg.condition_state;
            bool movementStateError = c_state_buffer[bufferIndex].movement_state == stateMsg.movement_state;

            // Dont need to do anything more if errors are in acceptable range and debug reconciliation is disabled
            if (posError.sqrMagnitude < 0.0000001f && rotError < 0.00001f && conditionStateError && movementStateError && !IsDebugReconciliationEnabled) { return; }
            
            // ==== If code reaches this point reconciliation is needed ==== 
            // Debug.Log($"Player {OwnerClientId} needs to reconcile for tick {stateMsg.tick_number}; Rewinding {ticks_to_rewind} ticks;  Pos Error: {posError}, Rot Error: {rotError}");

            Vector3 prev_pos = transform.position + this.c_pos_error;
            Quaternion prev_rot = transform.rotation * this.c_rot_error;
            
            // If correction is disabled in debug settings, do nothing
            if (!IsCorrectionEnabled) {
                return;
            }
            
            // Reset to state from state msg
            transform.SetPositionAndRotation(stateMsg.position, stateMsg.rotation);

            c_state_buffer[bufferIndex] = new ClientState {
                position = stateMsg.position,
                rotation = stateMsg.rotation,
                condition_state = stateMsg.condition_state, 
                movement_state = stateMsg.movement_state,
            };

            // Sync scene to point of state message
            PhysicsManager.Instance.SyncTransforms();

            // Re-simulate player from necessary tick to current client tick 
            while (rewind_tick_number < c_tick_number)
            {
                // Step player with inputs from buffer
                bufferIndex = rewind_tick_number % CLIENT_BUFFER_SIZE;
                var rewindInput = c_input_buffer[bufferIndex];

                // We assume that the state sent in state msg does not change until current tick; If it does not we have to reconcile when next stateMsg arrives;
                if (stateMsg.condition_state == CharacterStates.CharacterConditions.Normal)
                {
                    if(stateMsg.movement_state == CharacterStates.CharacterMovementStates.Running || stateMsg.movement_state == CharacterStates.CharacterMovementStates.Idle )
                    {
                        _playerMovement.EarlyProcessAbility(rewindInput);
                        _playerMovement.ProcessAbility();
                        
                        PhysicsManager.Instance.SyncTransforms();
                    }
                }

                // Save new state
                c_state_buffer[bufferIndex] = new ClientState {
                    position = transform.position,
                    rotation = transform.rotation,
                    condition_state = stateMsg.condition_state,
                    movement_state = stateMsg.movement_state,
                };

                ++rewind_tick_number;
            }

            // If more than 2ms apart, snap to pos
            if ((prev_pos - transform.position).sqrMagnitude >= 8.0f) {
                this.c_pos_error = Vector3.zero;
                this.c_rot_error = Quaternion.identity;
            }
            else {
                this.c_pos_error = prev_pos - transform.position;
                this.c_rot_error = Quaternion.Inverse(transform.rotation) * prev_rot;
            }

            UpdateAnimators();
        }

        /// <summary>
        /// Compare state and calculate smoothed pos/rot
        /// </summary>
        private void StepRemoteClientMovement(StateMessage stateMsg)
        {
            Assert.IsTrue(!IsLocalPlayer);
            if (stateMsg == null) { return; }

            Vector3 prev_pos = transform.position + this.c_pos_error;
            Quaternion prev_rot = transform.rotation * this.c_rot_error;
            
            Vector3 posError = stateMsg.position - transform.position;
            float rotError = 1.0f - Quaternion.Dot(stateMsg.rotation, transform.rotation);

            if (posError.sqrMagnitude > 0.0001f || rotError > 0.0001f)
            {
                transform.SetPositionAndRotation(stateMsg.position, stateMsg.rotation);
                
                // If more than 2ms apart, snap to pos
                if ((prev_pos - transform.position).sqrMagnitude >= 8.0f) {
                    this.c_pos_error = Vector3.zero;
                    this.c_rot_error = Quaternion.identity;
                }
                else {
                    this.c_pos_error = prev_pos - transform.position;
                    this.c_rot_error = Quaternion.Inverse(transform.rotation) * prev_rot;
                }
            }
        }

        // Set Character Model to smoothed position and rotation
        private void StepSmoothedPosRot()
        {
            // No smoothing needed for host
            if (IsServer && IsLocalPlayer) {
                return;
            }
            
            this.c_pos_error *= 0.95f;
            this.c_rot_error = Quaternion.Slerp(this.c_rot_error, Quaternion.identity, 0.1f);

            if (CharacterModel != null) {
                CharacterModel.transform.SetPositionAndRotation(transform.position + c_pos_error, transform.rotation * c_rot_error);
            }
        }
        
        /// <summary>
        /// Set position and rotation of a ServerDisplayPlayer mesh;
        /// Instantiate it if its null;
        /// </summary>
        private void ToggleServerDisplayPlayerAndSetState(bool isEnabled, Vector3 pos, Quaternion rot)
        {
            // We are not using debug display player so return
            if (s_display_transform == null && !isEnabled) {
                return;
            }
            
            // We want to use display player but its missing, so instantiate
            if (s_display_transform == null && isEnabled) {
                var playerObj = Instantiate(ServerDisplayPlayer, transform.position, transform.rotation);
                s_display_transform = playerObj.GetComponent<Transform>();
            }

            // Toggle active in hierarchy -> This enables switching on/off at runtime
            if (s_display_transform.gameObject.activeInHierarchy != isEnabled) {
                s_display_transform.gameObject.SetActive(isEnabled);
            }

            // Set position / rotation
            if (isEnabled) { s_display_transform.SetPositionAndRotation(pos, rot); }
        }

        #endregion
        
        #region SERVER
        private void StepServerPlayer()
        {
            Assert.IsTrue(IsServer);

            // TODO: Combine this and get most recent msg into method
            if (s_input_msg_queue.Count < 1) {
                return;
            }

            // Get most recent inputMessage
            var inputMsg = new InputMessage();
            while (s_input_msg_queue.Count > 0) {
                inputMsg = s_input_msg_queue.Dequeue();
            }

            // Check if msg contains input that server has not processed yet
            if ((inputMsg.client_tick_number <= s_last_processed_tick)) {
                Debug.Log("No new inputs in inputMsg");
                return;
            }
            
            // Get amount of ticks that the server has not processed
            var ticks_to_process = (uint)inputMsg.client_tick_number - s_last_processed_tick;
            if (ticks_to_process < 1) {
                Debug.LogWarning($"Player {OwnerClientId} has no ticks to process!"); return;
            }
           
            s_last_processed_tick = (uint)inputMsg.client_tick_number;
            Vector3 prev_pos = transform.position + this.c_pos_error;
            Quaternion prev_rot = transform.rotation * this.c_rot_error;
            
            StateMessage stateMsg = new StateMessage();
            // Process all new inputs for server
            for (uint i = 0; i < ticks_to_process; i++)
            {
                var inputIndex = (inputMsg.player_inputs.Length - ticks_to_process) + i;

                if (inputIndex < 0 || inputIndex > inputMsg.player_inputs.Length - 1) {
                    Debug.LogWarning($"InputIndex Error: Server does not have enough inputs to process for player {OwnerClientId}!");
                    continue; // skip this iteration of the for loop if index is not valid
                }
                
                EarlyProcessAbilities(inputMsg.player_inputs[inputIndex]);
                ProcessAbilities();
                LateProcessAbilities();
                
                PhysicsManager.Instance.StepPhysicsScene(inputMsg.delta_time);

                // Create stateMsg form resulting position and calculate tick for each state
                stateMsg = new StateMessage {
                    delta_time = inputMsg.delta_time,
                    tick_number = inputMsg.client_tick_number - (ticks_to_process - i) + 1,
                    position = this.transform.position,
                    rotation = this.transform.rotation,
                    condition_state = ConditionStateMachine.CurrentState,
                    movement_state = MovementStateMachine.CurrentState,
                };
            }

            // Only send every second tick; 25/sec currently
            if (stateMsg.tick_number % 2 == 0) {
                EnqueueStateMsgClientRpc(stateMsg);
            }
            
            // If more than 2ms apart, snap to pos
            if ((prev_pos - transform.position).sqrMagnitude >= 8.0f) {
                this.c_pos_error = Vector3.zero;
                this.c_rot_error = Quaternion.identity;
            }
            else {
                this.c_pos_error = prev_pos - transform.position;
                this.c_rot_error = Quaternion.Inverse(transform.rotation) * prev_rot;
            }
            
            UpdateAnimators();

        }
        #endregion
        
        #region SERVER RPC's
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
        private void EnqueueInputMsgServerRpc(InputMessage inputMessage)
        {
            if (!IsServer) {
                return;
            }

            s_input_msg_queue.Enqueue(inputMessage);
        }
        #endregion

        #region CLIENT RPC's
        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        public void EnqueueStateMsgClientRpc(StateMessage msg)
        {
            if (!IsClient) {
                return;
            }

            c_movement_state_msg_queue.Enqueue(msg);
        }
        #endregion
        
        #region ABILITY_FUNCTIONS
        /// <summary>
        /// Calls all registered abilities' Early Process methods
        /// </summary>
        protected virtual void EarlyProcessAbilities(PlayerInputs inputs, bool checkPredictability = false)
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                // Ability either turned off or not initalized, so do nothing
                if (!ability.enabled ||!ability.AbilityInitialized) {
                    return;
                }

                // If checkPredictable, we only want to execute predictable abilities
                if (checkPredictability) {
                    if (!ability.AbilityPredicted) {
                        return;
                    }
                }
                
                ability.EarlyProcessAbility(inputs);
            }
        }

        /// <summary>
        /// Calls all registered abilities' Process methods
        /// </summary>
        protected virtual void ProcessAbilities(bool checkPredictability = false)
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                // Ability either turned off or not initalized, so do nothing
                if (!ability.enabled ||!ability.AbilityInitialized) {
                    return;
                }

                // If checkPredictable, we only want to execute predictable abilities
                if (checkPredictability) {
                    if (!ability.AbilityPredicted) {
                        return;
                    }
                }
                
                ability.ProcessAbility();
            }
        }

        /// <summary>
        /// Calls all registered abilities' Late Process methods
        /// </summary>
        protected virtual void LateProcessAbilities(bool checkPredictability = false)
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                // Ability either turned off or not initalized, so do nothing
                if (!ability.enabled ||!ability.AbilityInitialized) {
                    return;
                }

                // If checkPredictable, we only want to execute predictable abilities
                if (checkPredictability) {
                    if (!ability.AbilityPredicted) {
                        return;
                    }
                }
                
                ability.LateProcessAbility();
            }
        }
        /// <summary>
        /// Caches abilities if necessary
        /// </summary>
        protected virtual void CacheAbilitiesAtInit()
        {
            // we grab all abilities at our level
            _characterAbilities = this.gameObject.GetComponents<CharacterAbility>();
        }
        
        /// <summary>
        /// Calls the init function on every ability
        /// </summary>
        protected virtual void InitalizeAbilities()
        {
            foreach (CharacterAbility ability in _characterAbilities)
            {
                ability.InitalizeAbility();
            }
        }
        
        /// <summary>
        /// A method to check whether a Character has a certain ability or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T FindAbility<T>() where T:CharacterAbility
        {
            CacheAbilitiesAtInit();

            Type searchedAbilityType = typeof(T);
            
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability is T characterAbility)
                {
                    return characterAbility;
                }
            }

            return null;
        }
        
        /// <summary>
        /// A method to check whether a Character has a certain ability or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public CharacterAbility FindAbilityByString(string abilityName)
        {
            CacheAbilitiesAtInit();
            
            foreach (CharacterAbility ability in _characterAbilities)
            {
                if (ability.GetType().Name == abilityName)
                {
                    return ability;
                }
            }

            return null;
        }
        #endregion

        #region ANIMATION
        /// <summary>
        /// Binds an animator to this character
        /// </summary>
        public virtual void AssignAnimator()
        {
            if (_animatorInitialized) {
                return;
            }
            
            _animatorParameters = new HashSet<int>();
            
            if (CharacterAnimator != null) {
                _animator = CharacterAnimator;
            }
            else {
                _animator = this.gameObject.GetComponent<Animator>();
            }
            
            if (CharacterNetAnimator != null) {
                _netAnimator = CharacterNetAnimator;
            }
            else {
                _netAnimator = this.gameObject.GetComponent<NetworkAnimator>();
            }

            if (_animator != null)
            {
                InitializeAnimatorParameters();
            }

            _animatorInitialized = true;
        }

        /// <summary>
        /// Initializes the animator parameters.
        /// </summary>
        protected virtual void InitializeAnimatorParameters()
        {
            if (_animator == null) { return; }
            
        }
        
        /// <summary>
        /// This is called at Update() and sets each of the animators parameters to their corresponding State values
        /// </summary>
        protected virtual void UpdateAnimators()
        {
            foreach (CharacterAbility ability in _characterAbilities) 
            {
                if (ability.enabled && ability.AbilityInitialized) {	
                    ability.UpdateAnimator();
                }
            }
        }
        #endregion

        
        /// <summary>
        /// Sets the player ID
        /// </summary>
        /// <param name="newPlayerID">New player ID.</param>
        public virtual void SetPlayerID(string newPlayerID)
        {
           // PlayerID = newPlayerID;
           // SetInputManager();
        }

        /// <summary>
        /// Makes the player respawn at the location passed in parameters
        /// </summary>
        /// <param name="spawnPoint">The location of the respawn.</param>
        public virtual void RespawnAt(Transform spawnPoint)
        {
            
        }
        
        /// <summary>
        /// Use this method to change the character's condition for a specified duration, and resetting it afterwards.
        /// You can also use this to disable gravity for a while, and optionally reset forces too.
        /// </summary>
        /// <param name="newCondition"></param>
        /// <param name="duration"></param>
        public virtual void ChangeCharacterConditionTemporarily(CharacterStates.CharacterConditions newCondition,
            float duration)
        {
            StartCoroutine(ChangeCharacterConditionTemporarilyCo(newCondition, duration));
        }

        /// <summary>
        /// Coroutine handling the temporary change of condition mandated by ChangeCharacterConditionTemporarily
        /// </summary>
        /// <param name="newCondition"></param>
        /// <param name="duration"></param>
        /// <param name="resetControllerForces"></param>
        /// <param name="disableGravity"></param>
        /// <returns></returns>
        protected virtual IEnumerator ChangeCharacterConditionTemporarilyCo(
            CharacterStates.CharacterConditions newCondition,
            float duration)
        {
            ApplyCharacterCondition(newCondition);
            yield return new WaitForSeconds(duration);
            ResetCharacterCondition();
        }

        private void ApplyCharacterCondition(CharacterStates.CharacterConditions newCondition)
        {
            if (!IsServer) {
                return;
            }
            
            switch (newCondition)
            {
                case CharacterStates.CharacterConditions.Stunned:
                    _netAnimator.SetTrigger("OnStun");
                    ConditionStateMachine.ChangeState(CharacterStates.CharacterConditions.Stunned);
                    MovementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.None);
                    break;
                default:
                    return;
                    break;
            }
        }

        private void ResetCharacterCondition()
        {
            if (!IsServer) {
                return;
            }

            if (ConditionStateMachine.CurrentState == CharacterStates.CharacterConditions.Stunned) {
                _netAnimator.SetTrigger("OnStunReset");
            }
            
            ConditionStateMachine.ChangeState(CharacterStates.CharacterConditions.Normal);
            MovementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
        }

        /// <summary>
        /// Freezes this character.
        /// </summary>
        public virtual void Freeze()
        {
            
        }

        /// <summary>
        /// Unfreezes this character
        /// </summary>
        public virtual void UnFreeze()
        {
            
        }
        
        /// <summary>
        /// Called to disable the player (at the end of a level for example. 
        /// It won't move and respond to input after this.
        /// </summary>
        protected virtual void Disable()
        {
            //this.enabled = false;
            LinkedPlayerController.enabled = false;			
            LinkedCharacterController.enabled = false;

            if (IsLocalPlayer) {
                LinkedInputHandler.enabled = false;
            }

        }

        protected virtual void OnGameOver(int teamId)
        {
            Disable();
        }

        /// <summary>
        /// Called when the Character dies. 
        /// Calls every abilities' Reset() method, so you can restore settings to their original value if needed
        /// </summary>
        public virtual void ResetAbilities()
        {
            if (IsServer)
            {
                foreach (CharacterAbility ability in _characterAbilities)
                {
                    ability.Cooldown.SetCooldownReady();
                }
            }

            if (IsLocalPlayer) {
                LinkedAbilityHandler.ResetAbilityCooldowns();
            }
        }

        
        protected virtual void OnDeath(float respawnTime)
        {
            Disable();
            LinkedFXHandler.PlaySpawnFX(Net_TeamID.Value);
            
            if (IsLocalPlayer) {
                LinkedInputHandler.RumbleController(2f, 0.75f);
            }
        }
        
        protected virtual void OnRespawn()
        {
            LinkedPlayerController.enabled = true;
            LinkedCharacterController.enabled = true;
            
            ResetAbilities();
            
            if (IsLocalPlayer) {
                LinkedInputHandler.enabled = true;
                LinkedInputHandler.RumbleController(0.4f, 0.5f);

            }
        }
        
        protected virtual void OnHit(float health)
        {
            if (IsLocalPlayer) {
                LinkedInputHandler.RumbleController(0.5f, 0.75f);
            }
        }
    }
}