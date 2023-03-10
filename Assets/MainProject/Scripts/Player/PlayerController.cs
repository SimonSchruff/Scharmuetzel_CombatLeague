
/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;


namespace MainProject.Scripts.Player
{



    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")] [SerializeField] private float MOVE_SPEED;
        [SerializeField] private float ROT_SPEED;

        //[Space(10)] 
        [Header("Client-Side-Prediction")] [SerializeField]
        private bool IsCorrectionEnabled;


        // DEBUG AREA
        [Serializable]
        public struct DEBUG_SETTINGS
        {
            [Tooltip("Whether or not the servers state of the player is displayed;")]
            public bool IsServerDisplayPlayerEnabled;

            public GameObject ServerDisplayPlayerPrefab;
            [HideInInspector] public Transform s_display_transform;

            [Tooltip("Reconciles from last received state message even if there is no state error;")]
            public bool IsDebugReconciliationEnabled;
        }

        [Space(20)] public DEBUG_SETTINGS DebugSettings;

        // PRIVATE VARIABLES

        // COMPONENTS
        private PlayerInputHandler _inputHandler;
        private PlayerStateFactory _stateFactory;
        private PlayerAnimationHandler _animHandler;
        private Rigidbody _rb;
        private CharacterController _charController;
        private Transform _transform;

        // Input
        private Vector2 _moveInput;
        private List<PlayerInputs> c_redundant_inputs = new List<PlayerInputs>();

        // Network Variables
        private NetworkVariable<Vector3> net_Position = new NetworkVariable<Vector3>();
        private NetworkVariable<Vector3> net_Rotation = new NetworkVariable<Vector3>();
        public NetworkVariable<int> Net_TeamID = new NetworkVariable<int>();

        // Local Client
        private ulong c_localId;

        private uint c_tick_number = 1; // Start ticking at 1, so tick - input.length is not negative
        private uint c_last_received_state_tick = 0;

        private const int CLIENT_BUFFER_SIZE = 1024;
        private ClientState[] c_state_buffer;
        private PlayerInputs[] c_input_buffer;

        private Queue<StateMessage> c_state_msg_queue;

        // private ClientState c_current_state;

        private float _camRotation;

        // SERVER
        private Queue<InputMessage> s_input_msg_queue;
        private PlayerInputs[] s_input_buffer;
        private uint s_last_processed_tick = 0;

        // EVENTS

        public override void OnNetworkSpawn()
        {
            _camRotation = Camera.main.transform.eulerAngles.y;

            _transform = GetComponent<Transform>();
            _rb = GetComponent<Rigidbody>();
            _charController = GetComponent<CharacterController>();
            _animHandler = GetComponent<PlayerAnimationHandler>();

            c_state_buffer = new ClientState[CLIENT_BUFFER_SIZE];
            c_input_buffer = new PlayerInputs[CLIENT_BUFFER_SIZE];
            c_state_msg_queue = new Queue<StateMessage>();

            if (IsLocalPlayer)
            {
                InitLocalPlayer();
            }

            if (IsServer)
            {
                InitServer();
            }

            NetworkManager.NetworkTickSystem.Tick += TickPlayer;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.NetworkTickSystem.Tick -= TickPlayer;
        }

        private void TickPlayer()
        {
            if (IsLocalPlayer)
            {
                // Get Input and send to server
                GetCurrentInputAndSendToServer();

                // Predict and save state to buffer; If server -> predicting is not necessary, bc there is no delay anyway
                if (!IsServer)
                {
                    PredictOnLocalClient();
                }
            }

            if (IsServer)
            {
                // Step server player with input, and send out state message
                StepServerPlayer();
            }
            else
            {
                if (IsLocalPlayer)
                {
                    // TODO: Reconcile if necessary
                    ReconcileLocalClient();
                }
                else
                {
                    // TODO: Handle state message, interpolate or sth similar
                    StepRemoteClient();
                }
            }

            c_tick_number++;
        }

        #region LOCAL_PLAYER

        private void InitLocalPlayer()
        {
            Assert.IsTrue(IsLocalPlayer);

            // Get Components
            _inputHandler = GetComponent<PlayerInputHandler>();
            _stateFactory = GetComponent<PlayerStateFactory>();

            var serverPlayer = Instantiate(DebugSettings.ServerDisplayPlayerPrefab, new Vector3(0, 0, 0),
                Quaternion.identity);
            DebugSettings.s_display_transform = serverPlayer.GetComponent<Transform>();

            // Init Vars
            c_localId = NetworkManager.Singleton.LocalClientId;
            // Subscribe to events
        }

        private void GetCurrentInputAndSendToServer()
        {
            Assert.IsTrue(IsLocalPlayer);

            var dt = Time.fixedDeltaTime;

            // Get current player input
            // TODO: Implement KeyStates to detect if button is pressed/held/released
            PlayerInputs currentPlayerInputs = new PlayerInputs()
            {
                delta_time = dt,
                left_stick_input = _inputHandler.LeftStickInput,
                basic_attack_input = _inputHandler.BasicAttackKeyState,
            };

            // Add current input to redundant input list
            c_redundant_inputs.Add(currentPlayerInputs);

            // Limit list length to the last acknowledged state by server or 64 to prevent maxSendSize Error; Server does not use last received state tick, so prevent buffer overflow;
            var redundantInputCount = Math.Clamp(IsServer ? 8 : c_tick_number - c_last_received_state_tick, 1, 64);
            while (c_redundant_inputs.Count > redundantInputCount)
            {
                c_redundant_inputs.RemoveAt(0);
            }

            // Create InputMsg
            InputMessage inputMsg = new InputMessage
            {
                client_tick_number = c_tick_number,
                player_inputs = c_redundant_inputs.ToArray(),
            };

            // TODO: Optimize to only send inputMsg if there is relevant input; 
            // Send input to server
            SendInputServerRpc(inputMsg);

            // Record input in local input buffer
            var bufferIndex = c_tick_number % CLIENT_BUFFER_SIZE;
            c_input_buffer[bufferIndex] = currentPlayerInputs;

            // Clear buttons
            _inputHandler.ClearButtonInput();
        }

        private void PredictOnLocalClient()
        {
            Assert.IsTrue(IsLocalPlayer);

            // Get current input
            var bufferIndex = c_tick_number % CLIENT_BUFFER_SIZE;
            var inputs = c_input_buffer[bufferIndex];

            // Step player movement
            SimpleMoveStepPlayer(inputs, 0.02f, _charController);

            // Save resulting state
            c_state_buffer[bufferIndex] = new ClientState
            {
                position = _rb.position,
                rotation = _rb.rotation,
            };
        }

        private void ReconcileLocalClient()
        {
            Assert.IsTrue(IsLocalPlayer);

            if (c_state_msg_queue.Count < 1)
            {
                return;
            }

            // Get most recent stateMsg
            var stateMsg = new StateMessage();
            while (c_state_msg_queue.Count > 0)
            {
                stateMsg = c_state_msg_queue.Dequeue();
            }

            c_last_received_state_tick = stateMsg.tick_number;

            if (!IsCorrectionEnabled)
            {
                return;
            }

            var bufferIndex = stateMsg.tick_number % CLIENT_BUFFER_SIZE;
            // Calculate Rotation or Position Error between client and server pos/rot
            var posError = c_state_buffer[bufferIndex].position - stateMsg.position;
            var rotError = 1.0f - Quaternion.Dot(stateMsg.rotation, c_state_buffer[bufferIndex].rotation);

            // Dont need to do anything more if errors are in acceptable range and debug reconciliation is disabled
            if (posError.sqrMagnitude < 0.0000001f && rotError < 0.00001f &&
                !DebugSettings.IsDebugReconciliationEnabled)
            {
                return;
            }

            var ticks_to_rewind = c_tick_number - stateMsg.tick_number;
            var rewind_tick_number = stateMsg.tick_number;
            var sendRate = 0.02f / 1000;

            // Reconciliation needed
            Debug.Log(
                $"Player {OwnerClientId} needs to reconcile for tick {stateMsg.tick_number}; Rewinding {ticks_to_rewind} ticks;  Pos Error: {posError}, Rot Error: {rotError}");

            // Reset to state from state msg

            ToggleServerDisplayPlayerAndSetState(DebugSettings.IsServerDisplayPlayerEnabled, stateMsg.position,
                stateMsg.rotation);

            // BUG: Client snaps back to wrong positions
            _rb.position = stateMsg.position;
            _rb.rotation = stateMsg.rotation;

            // Re-simulate player from necessary tick to current client tick 
            while (rewind_tick_number < c_tick_number)
            {
                // Step player with inputs from buffer
                bufferIndex = rewind_tick_number % CLIENT_BUFFER_SIZE;

                // TODO: Step player with fraction of delta time to avoid character controller bugs, but shitty "solution"
                var rewindInput = c_input_buffer[bufferIndex];
                // rewindInput.delta_time = rewindInput.delta_time / (ticks_to_rewind * 250);

                SimpleMoveStepPlayer(rewindInput, sendRate, _charController);

                // TODO: Move by result approach
                //var result = MovePlayer(rewindInput, _charController);
                // _rb.MovePosition(result.position);
                // _rb.MoveRotation(result.rotation);
                //c_state_buffer[bufferIndex] = result;


                // Save new state
                c_state_buffer[bufferIndex] = new ClientState
                {
                    position = _rb.position,
                    rotation = _rb.rotation,
                };


                ++rewind_tick_number;
            }
        }

        private void ToggleServerDisplayPlayerAndSetState(bool isEnabled, Vector3 pos, Quaternion rot)
        {
            Assert.IsTrue(IsLocalPlayer);

            // Toggle active of gameObject
            if (DebugSettings.s_display_transform.gameObject.activeInHierarchy != isEnabled)
            {
                DebugSettings.s_display_transform.gameObject.SetActive(isEnabled);
            }

            // Set position / rotation
            if (isEnabled)
            {
                DebugSettings.s_display_transform.position = pos;
                DebugSettings.s_display_transform.rotation = rot;
            }
        }

        #endregion

        #region SERVER

        private void InitServer()
        {
            Assert.IsTrue(IsServer);

            s_input_msg_queue = new Queue<InputMessage>();
            s_input_buffer = new PlayerInputs[CLIENT_BUFFER_SIZE];
        }

        private void StepServerPlayer()
        {
            Assert.IsTrue(IsServer);

            if (s_input_msg_queue.Count < 1)
            {
                return;
            }

            // Get most recent inputMessage
            var inputMsg = new InputMessage();
            while (s_input_msg_queue.Count > 0)
            {
                inputMsg = s_input_msg_queue.Dequeue();
            }

            // Check if msg contains input that server has not processed yet
            if ((inputMsg.client_tick_number <= s_last_processed_tick))
            {
                Debug.Log("No new inputs in inputMsg");
                return;
            }

            // Get amount of ticks that the server has not "seen"
            var ticks_to_process = inputMsg.client_tick_number - s_last_processed_tick;

            StateMessage stateMsg = new StateMessage();

            // Process all new inputs for server
            for (uint i = 0; i < ticks_to_process; i++)
            {
                var inputIndex = (inputMsg.player_inputs.Length - ticks_to_process) + i;
                // Basic Attack
                OnBasicAttack(inputMsg.player_inputs[inputIndex]);

                // Step player movement with input of ticks
                SimpleMoveStepPlayer(inputMsg.player_inputs[inputIndex], 0.02f, _charController);


                // var result = MovePlayer(inputMsg.player_inputs[(inputMsg.player_inputs.Length - ticks_to_process) + i ], _charController);
                //_rb.MovePosition(result.position);
                // _rb.rotation = (result.rotation);
                // Physics.SyncTransforms();

                // Create stateMsg form resulting position and calculate tick for each state
                stateMsg = new StateMessage
                {
                    tick_number = (inputMsg.client_tick_number - (ticks_to_process - i)) + 1,
                    position = _rb.position,
                    rotation = _rb.rotation,
                };
            }

            net_Position.Value = stateMsg.position;
            net_Rotation.Value = stateMsg.rotation.eulerAngles;

            EnqueueStateMsgClientRpc(stateMsg);

            s_last_processed_tick = inputMsg.client_tick_number;
        }

        #endregion

        private void StepRemoteClient()
        {
            Assert.IsTrue(!IsLocalPlayer);

            if (c_state_msg_queue.Count < 1)
            {
                return;
            }

            // Get most recent stateMsg
            var stateMsg = new StateMessage();
            while (c_state_msg_queue.Count > 0)
            {
                stateMsg = c_state_msg_queue.Dequeue();
            }

            // Step remote client
            _rb.position = (stateMsg.position);
            _rb.rotation = (stateMsg.rotation);

            // TODO: Rollback for abilities/movement
        }

        #region SERVER_RPC's

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void SendInputServerRpc(InputMessage inputMessage)
        {
            if (!IsServer)
            {
                return;
            }

            s_input_msg_queue.Enqueue(inputMessage);
        }

        #endregion


        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void EnqueueStateMsgClientRpc(StateMessage msg)
        {
            if (!IsClient)
            {
                return;
            }

            c_state_msg_queue.Enqueue(msg);
        }

        private void SimpleMoveStepPlayer(PlayerInputs inputs, float sendRate, CharacterController charController)
        {
            // Get move input and dt
            Vector2 leftStickInput = inputs.left_stick_input;
            float dt = inputs.delta_time;

            // From h33p Unet Controller
            // var sendUpdate = Mathf.Max(1, Mathf.RoundToInt(settings.sendRate / Time.fixedDeltaTime));
            // var sendUpdate = Mathf.Max(1, Mathf.RoundToInt(sendRate / dt));

            var sendUpdate = sendRate / dt;
            dt *= sendUpdate;
            // print($"DeltaTime {dt} with sendUpdate {sendUpdate}");

            // Switch to idle if no move input and return
            if (leftStickInput.sqrMagnitude == 0)
            {
                if (IsLocalPlayer)
                {
                    _stateFactory.SwitchMovementState(CharacterStates.CharacterMovementStates.Idle);
                }

                return;
            }

            // If code reaches this point we are moving
            if (IsLocalPlayer)
            {
                _stateFactory.SwitchMovementState(CharacterStates.CharacterMovementStates.Idle);
            }

            // Get forward direction
            Vector3 forward = charController.gameObject.transform.forward * leftStickInput.magnitude;

            // Calculate target rotation
            float angle = Mathf.Atan2(leftStickInput.x, leftStickInput.y) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Lerp(charController.gameObject.transform.rotation,
                Quaternion.Euler(0f, angle, 0f),
                dt * ROT_SPEED);

            // Move player and step simulation
            // TODO: probably should use transform.rotate here
            charController.gameObject.transform.rotation = rot;
            charController.Move(forward * MOVE_SPEED * dt);
            PhysicsManager.Instance.StepPhysicsScene(dt);
        }


        private bool is_basic_attacking = false;

        private void OnBasicAttack(PlayerInputs inputs)
        {
            var attackState = inputs.basic_attack_input;

            // Input not relevant
            if (attackState == PlayerInputHandler.KeyState.off)
            {
                return;
            }

            print($"Player {OwnerClientId} Basic Attack state {is_basic_attacking}!");


            // Attack already being executed
            if (is_basic_attacking == true)
            {
                return;
            }

            // Execute Attack
            is_basic_attacking = true;
            float duration = _animHandler.OnBasicAttack();
            print($"Player {OwnerClientId} Basic Attack duration {duration}!");
            StartCoroutine(ResetBasicAttack(duration));
        }

        private IEnumerator ResetBasicAttack(float sec)
        {
            yield return new WaitForSeconds(sec);
            is_basic_attacking = false;
        }

        private ClientState MovePlayer(PlayerInputs inputs, CharacterController charController)
        {
            // Get move input and dt
            Vector2 leftStickInput = inputs.left_stick_input;
            float dt = inputs.delta_time;

            ClientState initial_state = new ClientState
            {
                position = charController.transform.position,
                rotation = charController.transform.rotation,
            };

            // Switch to idle if no move input and return
            if (leftStickInput.sqrMagnitude == 0)
            {
                if (IsLocalPlayer)
                {
                    _stateFactory.SwitchMovementState(CharacterStates.CharacterMovementStates.Idle);
                }

                return initial_state;
            }

            // If code reaches this point we are moving
            if (IsLocalPlayer)
            {
                _stateFactory.SwitchMovementState(CharacterStates.CharacterMovementStates.Running);
            }


            // Get forward direction
            Vector3 forward = charController.gameObject.transform.forward * leftStickInput.magnitude;

            // Calculate target rotation
            float angle = Mathf.Atan2(leftStickInput.x, leftStickInput.y) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Lerp(charController.gameObject.transform.rotation,
                Quaternion.Euler(0f, angle, 0f),
                dt * ROT_SPEED);

            // Move player and step simulation
            // TODO: probably should use transform.rotate here
            charController.gameObject.transform.rotation = rot;
            charController.Move(forward * MOVE_SPEED * dt);

            PhysicsManager.Instance.StepPhysicsScene(dt);

            var resulting_state = new ClientState
            {
                position = charController.transform.position,
                rotation = charController.transform.rotation,
            };

            print($"Result Pos {resulting_state.position}; Init Pos {initial_state.position}");

            charController.transform.position = initial_state.position;
            charController.transform.rotation = initial_state.rotation;

            return resulting_state;
        }

        private void ProcessPlayerInput(PlayerInputs inputs)
        {
            // Check for Basic Attack Input
            var basicAttackInput = inputs.basic_attack_input;
            if (basicAttackInput == PlayerInputHandler.KeyState.press ||
                basicAttackInput == PlayerInputHandler.KeyState.held ||
                basicAttackInput == PlayerInputHandler.KeyState.release)
            {
                // Basic Attack was pressed in last frame and should be executed
            }

            // Check for movement input
            var moveInput = inputs.left_stick_input;
            if (moveInput.sqrMagnitude != 0)
            {
                // Player should move if code reaches this point

            }
        }

    }
}
*/