using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class SpinAttack : CharacterAbility
    {
        [Header("Settings")]
        [SerializeField] private float _timeToEnableCollider;
        [SerializeField] private float _colliderRadius;
        [SerializeField] private float _totalAbilityTime;
        [SerializeField] private LayerMask _collisionLayerMask;

        [Header("Sprites")]
        [SerializeField] private Image AimDisplaySprite;
        
        // Animation
        protected const string _castAnimationParameterName = "Cast03";
        protected int _castAnimationParameter;
        
        [Header("Damage")]
        [SerializeField] private DamageType Damage;
        
        // [Header("Cooldown")] 
        // public AbilityCooldown Cooldown;
        
        private float _castTimer;
        private bool _isCasting = false;
        private bool _isAttackExecuted = false;
        
        /// <summary>
        ///     On init we initialize our cooldown and feedback
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();
            Cooldown.Initialization();
        }
        
        protected override void HandleInput(PlayerInputs inputs)
        {
            // Start dash if server and has input and not already dashing
            if (!IsServer) {
                return;
            }

            // Do not use ability if has cooldown, or is not authorized because of movement/condition states
            if (!AbilityAuthorized || !Cooldown.Ready() || _isCasting) { return; }

            // if (inputs.cast_02_input == KeyState.press || inputs.cast_02_input == KeyState.held)
            if (inputs.cast_03_input == KeyState.release) {
                CastStart();
            }
        }
        
        public bool AimAbility(PlayerInputs inputs)
        {
            if (!IsLocalPlayer) { return false; }
            
            if (!AbilityAuthorized || !_abilityHandler.CheckIfAbilityIsReady(AbilityTypes.SpinAttack) || _isCasting) { return false; }

            if (inputs.cast_03_input == KeyState.press || inputs.cast_03_input == KeyState.held) {
                EnableHitAreaPreview();
                return true;
            }
            else {
                DisableHitAreaPreview();
                return false;
            }
        }
        
        protected virtual void CastStart()
        {
            if (!Cooldown.Ready() || !IsServer) { return; }
            
            Cooldown.Start();
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);
            _netAnimator.SetTrigger(_castAnimationParameter);
            _isCasting = true;
            
            NotifyAbilityStartedClientRPC();
        }
        
        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            EnableHitAreaPreview();
            _fxHandler.EnableWeaponTrailFX();
            _soundFXHandler.Play("SpinAttack");

            if (IsLocalPlayer) {
                _abilityHandler.StartCooldown(AbilityTypes.SpinAttack);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.SpinAttack);
            }
        }

       
        
        private void CastStop()
        {
            NotifyAbilityStopClientRPC();
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);

            _isAttackExecuted = false;
            _isCasting = false;
            _castTimer = 0f;
        }
        
        [ClientRpc]
        private void NotifyAbilityStopClientRPC()
        {
            _fxHandler.DisableWeaponTrailFX();
            DisableHitAreaPreview();
        }
        
        private void Update()
        {
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }
        
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_isCasting)
            {
                _castTimer += Time.fixedDeltaTime;
                
                if (_castTimer >= _timeToEnableCollider && !_isAttackExecuted) {
                    _isAttackExecuted = true;
                    EnableHitCollider();
                }
                
                if (_castTimer >= _totalAbilityTime) {
                    CastStop();
                }
            }
        }
        
        private void EnableHitCollider()
        {
            var colls = Physics.OverlapSphere(transform.position, _colliderRadius, _collisionLayerMask); 
            
            foreach (var c in colls) {
                if (c.TryGetComponent(out Character player)) {
                    if (player.Net_TeamID.Value != _character.Net_TeamID.Value) {
                        player.LinkedHealth.Damage(Damage, this.gameObject);
                    }
                }
            }
        }
        
        private void EnableHitAreaPreview()
        {
            if (!AimDisplaySprite.enabled) {
                AimDisplaySprite.enabled = true;
            }
        }

        private void DisableHitAreaPreview()
        {
            if (AimDisplaySprite.enabled) {
                AimDisplaySprite.enabled = false;
            }
        }
        
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_castAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castAnimationParameter);
        }
        
        private void OnDrawGizmos()
        {
            if (_isCasting && _castTimer >= _timeToEnableCollider)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _colliderRadius);
            }
        }
    }
}