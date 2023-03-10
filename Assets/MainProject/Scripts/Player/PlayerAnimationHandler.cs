using MainProject.Scripts.DataStructures.PlayerData;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class PlayerAnimationHandler : NetworkBehaviour
    {
        public AnimationClip BasicAttackAnim;
    
        private NetworkAnimator _networkAnimator;
        private PlayerStateFactory state_factory;

        private CharacterStates.CharacterMovementStates previous_movement_state;

        private int _idleHash;
        private int _runHash;
        private int _basicAttackHash;

        private int count = 1;

        public override void OnNetworkSpawn()
        {
            if (!IsClient) {
                return;
            }
        
            state_factory = GetComponent<PlayerStateFactory>();
            _networkAnimator = GetComponent<NetworkAnimator>();
        
            _idleHash = Animator.StringToHash("Idle"); 
            _runHash = Animator.StringToHash("Run");
            _basicAttackHash = Animator.StringToHash("BasicAttack");
        
            if (IsLocalPlayer) {
                state_factory.OnSwitchMoveState += OnPlayerStateChanged;
            }
        
        }

        private void OnPlayerStateChanged(CharacterStates.CharacterMovementStates newState)
        {
            //print($"Anim State Change {count++}");
            ResetTrigger();
            
            // Set new trigger
            switch (newState)
            {
                case CharacterStates.CharacterMovementStates.Running:
                    _networkAnimator.SetTrigger(_runHash);
                    break;
                case CharacterStates.CharacterMovementStates.Idle:
                    _networkAnimator.SetTrigger(_idleHash);
                    break;
            
            
                default:
                    break;
            }

            previous_movement_state = newState;
        }

        public float OnBasicAttack()
        {
            ResetTrigger();
        
            _networkAnimator.SetTrigger(_basicAttackHash);
            return BasicAttackAnim.length;
        }
    
        public void SetIdle()
        {
            ResetTrigger();
        
            _networkAnimator.SetTrigger(_idleHash);
        }

        private void ResetTrigger()
        {
            // Reset Old Trigger
            switch (previous_movement_state)
            {
                case CharacterStates.CharacterMovementStates.Running:
                    _networkAnimator.ResetTrigger(_runHash);
                    break;
                case CharacterStates.CharacterMovementStates.Idle:
                    _networkAnimator.ResetTrigger(_idleHash);
                    break;
                case CharacterStates.CharacterMovementStates.Dashing:
                    break;
                default:
                    break;
            }
        
            _networkAnimator.ResetTrigger(_basicAttackHash);
        }



        public void OnDisable()
        {
            if (IsLocalPlayer) {
                state_factory.OnSwitchMoveState -= OnPlayerStateChanged;
            }
        }
    }
}
