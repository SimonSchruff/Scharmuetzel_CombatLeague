using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    /// <summary>
    /// Changes states of player with server authority and gives access through current state variables;
    /// </summary>
    public class PlayerNetworkState : NetworkBehaviour
    {
        private Character _character;
        private StateMachine<CharacterStates.CharacterMovementStates> _charMovementStateMachine;
        private StateMachine<CharacterStates.CharacterConditions> _charConditionStateMachine;

        /// <summary>
        /// The current network movement state of the character
        /// </summary>
        public CharacterStates.CharacterMovementStates CurrentMovementState => net_CurrentMovementState.Value;
        
        /// <summary>
        /// The current network condition state of the character
        /// </summary>
        public CharacterStates.CharacterConditions CurrentConditionState => net_CurrentConditionState.Value;
      
        // Net vars for states
        private NetworkVariable<CharacterStates.CharacterMovementStates> net_CurrentMovementState = new NetworkVariable<CharacterStates.CharacterMovementStates>();
        private NetworkVariable<CharacterStates.CharacterConditions> net_CurrentConditionState = new NetworkVariable<CharacterStates.CharacterConditions>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer) {
                return;
            }
            
            // Get references
            _character = GetComponent<Character>();
            _charMovementStateMachine = _character.MovementStateMachine;
            _charConditionStateMachine = _character.ConditionStateMachine;

            // Subscribe to on state changed event
            _charMovementStateMachine.OnStateChange += OnMovementStateChanged;
            _charConditionStateMachine.OnStateChange += OnConditionStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) {
                return;
            }
            // Unsubscribe to on state changed event
            _charMovementStateMachine.OnStateChange -= OnMovementStateChanged;
            _charConditionStateMachine.OnStateChange -= OnConditionStateChanged;
        } 
        
        private void OnMovementStateChanged()
        {
            if (!IsServer) {
                return;
            }

            net_CurrentMovementState.Value = _charMovementStateMachine.CurrentState;
        }
        
        private void OnConditionStateChanged()
        {
            if (!IsServer) {
                return;
            }

            net_CurrentConditionState.Value = _charConditionStateMachine.CurrentState;

        }
    }
}