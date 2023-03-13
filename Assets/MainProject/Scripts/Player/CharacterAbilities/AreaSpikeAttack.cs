using System;
using System.Collections;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.CharacterAbilities
{ 
    public class AreaSpikeAttack : CharacterAbility
    {
        [Header("Settings")]
        [SerializeField] private float _timeToAttack;
        [SerializeField] private float _totalAbilityTime;
        [SerializeField] private LayerMask _collisionLayerMask;

        [Header("Sprites")]
        [SerializeField] private Image AimDisplaySprite;
        
        // Animation
        protected const string _castAnimationParameterName = "Cast02";
        protected int _castAnimationParameter;
        
        [Header("Damage")]
        [SerializeField] private DamageType Damage;
        
        // [Header("Cooldown")] 
        // public AbilityCooldown Cooldown;

        private Vector3 _boxCollSize = new Vector3(4.5f, 1f, 6.5f);
        private Vector3 _boxPosOffset = new Vector3(0f,1f, 5f);
        
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
            
            _character.LinkedHealth.OnDamageTaken += OnDamageReceived;
            _character.LinkedHealth.OnDeath += OnDeath;
        }
        
        public override void OnNetworkDespawn()
        {
            if (!_abilityInitialized) {
                return;
            }
            
            _character.LinkedHealth.OnDamageTaken -= OnDamageReceived;
            _character.LinkedHealth.OnDeath -= OnDeath;
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
            if (inputs.cast_02_input == KeyState.release) {
                CastStart();
            }

        }
        
        public bool AimAbility(PlayerInputs inputs)
        {
            if (!IsLocalPlayer) { return false; }
            
            if (!AbilityAuthorized || !_abilityHandler.CheckIfAbilityIsReady(AbilityTypes.GroundSpikes) || _isCasting) { return false; }

            if (inputs.cast_02_input == KeyState.press || inputs.cast_02_input == KeyState.held) {
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
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);
            _netAnimator.SetTrigger(_castAnimationParameter);
            _isCasting = true;
            
            NotifyAbilityStartedClientRPC();
        }
        
        private void CastAbility()
        {
            if (!IsServer) { return; }
            
            NotifyAbilityCastClientRPC();
            
            // Check for collisions and do damage to enemy players
            Vector3 pos = transform.position + transform.forward * _boxPosOffset.z + Vector3.up;
            var colls = Physics.OverlapBox(pos, _boxCollSize / 2, transform.rotation, _collisionLayerMask);

            foreach (var c in colls) {
                if (c.TryGetComponent(out Character player)) {
                    if (player.Net_TeamID.Value != _character.Net_TeamID.Value)
                    {
                        player.LinkedHealth.Damage(Damage, this.gameObject);
                    }
                }
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
        private void NotifyAbilityStartedClientRPC()
        {
            EnableHitAreaPreview();
            _soundFXHandler.Play("GroundSpikeStart");

            if (IsLocalPlayer) {
                _abilityHandler.StartCooldown(AbilityTypes.GroundSpikes);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.GroundSpikes);
            }
        }
        
        [ClientRpc]
        private void NotifyAbilityCastClientRPC()
        {
            _fxHandler.PlayGroundSpikeFX();
            _soundFXHandler.Play("GroundSpikeAttack");
            DisableHitAreaPreview();
        }
        
        [ClientRpc]
        private void NotifyAbilityStopClientRPC()
        {
            DisableHitAreaPreview();
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
        
        private void Update()
        {
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }

        /// <summary>
        /// The first of the 3 passes you can have in your ability. Think of it as EarlyUpdate() if it existed
        /// </summary>
        public override void EarlyProcessAbility(PlayerInputs inputs)
        {
            base.EarlyProcessAbility(inputs);
        }
        
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_isCasting)
            {
                _castTimer += Time.fixedDeltaTime;
                if (_castTimer >= _timeToAttack && !_isAttackExecuted) {
                    _isAttackExecuted = true;
                    CastAbility();
                }

                if (_castTimer >= _totalAbilityTime) {
                    CastStop();
                }
            }
        }

        private void InterruptAbility()
        {
            if (!_isCasting) { return; }
            
            CastStop();
        }
        
        private void OnDamageReceived(float health)
        {
            InterruptAbility();
        }

        private void OnDeath(float respawnTime)
        {
            InterruptAbility();
        }

        private void OnDrawGizmos()
        {
            if (_isCasting)
            {
                Vector3 pos = transform.position + transform.forward * _boxPosOffset.z + Vector3.up;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(pos, _boxCollSize);
                
            }
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_castAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castAnimationParameter);
        }

        
    }
}