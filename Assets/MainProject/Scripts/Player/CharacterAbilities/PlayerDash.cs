using System;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class PlayerDash : CharacterAbility
    {
        [Header("Dash")]
        [Tooltip("the direction of the dash, relative to the character")]
        public Vector3 DashDirection = Vector3.forward;
        [Tooltip("the distance to cover")]
        public float DashDistance = 10f;
        [Tooltip("the duration of the dash, in seconds")]
        public float DashDuration = 0.5f;
        [Tooltip("the curve to apply to the dash's acceleration")]
        public AnimationCurve DashCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        //[Header("Cooldown")]
        //public AbilityCooldown Cooldown;
        
        [Header("Damage")] 
        [Tooltip("if this is true, this character won't receive any damage while a dash is in progress")]
        public bool InvincibleWhileDashing = false; 
        
        protected bool _dashing;
        protected float _dashTimer;
        protected Vector3 _dashOrigin;
        protected Vector3 _dashDestination;
        protected Vector3 _newPosition;
        protected Vector3 _oldPosition;

        
        protected const string _dashingAnimationParameterName = "Dash";
        protected int _dashingAnimationParameter;

        /// <summary>
        /// Watches for input and starts a dash if needed
        /// </summary>
        protected override void HandleInput(PlayerInputs inputs)
        {
            base.HandleInput(inputs);

            // Start dash if server and has input and not already dashing
            if (inputs.dash_input == KeyState.off || _dashing || !IsServer) {
                return;
            }
            
            // Do not dash if has cooldown, or is not authorized because of movement/condition states
            if (!AbilityAuthorized || !Cooldown.Ready()) {
                return;
            }
            
            DashStart();
        }

        /// <summary>
        /// Starts a dash
        /// </summary>
        public virtual void DashStart()
        {
            Cooldown.Start();
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Dashing);
            _dashing = true;
            _dashTimer = 0f;
            _dashOrigin = this.transform.position;

            if (InvincibleWhileDashing)
            {
               _character.LinkedHealth.DamageDisabled();
            }
            
            if (IsServer) {
                NotifyAbilityStartedClientRPC();
            }
            
            _dashDestination = this.transform.position + transform.forward * DashDistance;
            
            _netAnimator.SetTrigger(_dashingAnimationParameter);
            
        }

        /// <summary>
        /// Stops the dash
        /// </summary>
        public virtual void DashStop()
        {
            Cooldown.Stop();
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            _dashing = false;
            
            if (InvincibleWhileDashing)
            {
                _character.LinkedHealth.DamageEnabled();
            }
        }

        public void Update()
        {
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }

        /// <summary>
        /// On process ability, we move our character if we're currently dashing
        /// </summary>
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_dashing)
            {
                if (_dashTimer < DashDuration) {
                    
                    _oldPosition = _dashTimer == 0 ? _dashOrigin : _newPosition;
                    _newPosition = Vector3.Lerp(_dashOrigin, _dashDestination, DashCurve.Evaluate(_dashTimer / DashDuration));
                    _dashTimer += Time.fixedDeltaTime;
                    _controller.MovePosition(this.transform.position + _newPosition - _oldPosition);
                }
                else
                {
                    DashStop();
                }
            }

        }
        
        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            if (IsLocalPlayer) {
                _abilityHandler.StartCooldown(AbilityTypes.Dash);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.Dash);
            }
            
            _fxHandler.StartPlayTrailFX(DashDuration, _character.Net_TeamID.Value);
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_dashingAnimationParameterName, AnimatorControllerParameterType.Trigger, out _dashingAnimationParameter);
        }

        public override void UpdateAnimator()
        {
        }
    }
}
