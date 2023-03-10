using System;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MainProject.Scripts.Player
{
    public class PlayerStateFactory : NetworkBehaviour
    {
        public event Action<CharacterStates.CharacterMovementStates> OnSwitchMoveState;
        public NetworkVariable<CharacterStates.CharacterMovementStates> _networkMovementState = new NetworkVariable<CharacterStates.CharacterMovementStates>();

        public CharacterStates.CharacterMovementStates CurrentMovementState = CharacterStates.CharacterMovementStates.Idle;

        private int count = 1;
        
        public void SwitchMovementState(CharacterStates.CharacterMovementStates newState)
        {
            
            if (newState == CurrentMovementState) {
                return;
            }

            // print($"State Factory Switch MoveState {count++}");
            
            UpdatePlayerStateServerRpc(newState);
            CurrentMovementState = newState;
            OnSwitchMoveState?.Invoke(newState);
        }
        
        #region SERVER_RPC
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerStateServerRpc(CharacterStates.CharacterMovementStates newState)
        {
            if (!IsServer) {
                return;
            }
            
            _networkMovementState.Value = newState;
        }
        #endregion
        
        
    }
}