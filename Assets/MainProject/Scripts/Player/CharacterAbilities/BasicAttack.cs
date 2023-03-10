using System;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class BasicAttack : CharacterAbility
    {
        [Header("Ability Settings")] 
        [Tooltip("The time the attack takes until its finished")] 
        public float AttackTime = 1.3f;
        public float ColliderEnableTime = 0.5f;
        [SerializeField] private Vector3 _boxCollSize; 
        [SerializeField] private Vector3 _boxPosOffset; 
        [SerializeField] private LayerMask _collisionLayerMask; 
        [Tooltip("The time from when the input for the next basic attack will be cached; This enabled two hits after each other")] 
        [SerializeField] private float _inputCacheThresholdMin = 0.8f;
        [SerializeField] private float _inputCacheThresholdMax = 1.2f;
        [SerializeField] private  DamageType Damage;
        // public PlayerWeapon Weapon;
        
        //[Header("Cooldown")]
        //public AbilityCooldown Cooldown;

        // Private vars
        private float _castTimer = 0f;
        private bool _isCasting = false;
        private bool _isAttackExecuted = false;
        private bool _inputCached;
        private bool _isAnimTriggerSet;
        // Animation
        private bool _isFirstAttackAnim = true;
        private const string _castAnimationParameterName = "BasicAttack";
        private const string _castAnimationParameterName2 = "BasicAttack2";
        private const string _castEndAnimationParameterName = "EndBasicAttack";
        private int _castAnimationParameter;
        private int _castAnimationParameter2;
        private int _castEndAnimationParameter;
        
        /// <summary>
        /// On init we initialize our cooldown and feedback
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();
            Cooldown.Initialization();
            // Weapon.Initalization(Damage, this.gameObject);
        }
        
        protected override void HandleInput(PlayerInputs inputs)
        {
            // Start dash if server and has input and not already dashing
            if (inputs.basic_attack_input == KeyState.off || !IsServer) {
                return;
            }
            
            // Do not cast if has cooldown, or is not authorized because of movement/condition states
            if (!AbilityAuthorized || !Cooldown.Ready() || _isCasting) {
                // Chache input if its within window
                if (_isCasting && _castTimer >= _inputCacheThresholdMin && _castTimer <= _inputCacheThresholdMax) {
                    _inputCached = true;
                }
                return;
            }
        
            CastStart();
        }

        protected virtual void CastStart()
        {
            if (!IsServer) { return; }
            
            Cooldown.Start();
            
            NotifyAbilityStartedClientRPC();
            
            // Set anim triggers
            if (!_isAnimTriggerSet) {
                _netAnimator.SetTrigger(_castAnimationParameter);
            }
            
            _isCasting = true;
            _isAnimTriggerSet = false;
            _isAttackExecuted = false;
            _inputCached = false;
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);
        }

        protected virtual void CastStop()
        {
            if (!IsServer) { return; }

            _castTimer = 0f;
            _isCasting = false;
            
            if (_inputCached) {
                CastStart();
                return;
            }
            
            _inputCached = false;
            _isAnimTriggerSet = false;
            _isFirstAttackAnim = true;
            _isAttackExecuted = false;
            _netAnimator.SetTrigger(_castEndAnimationParameter);
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
        }

        private void Update()
        {
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }

        public override void ProcessAbility()
        {
            if (!IsServer) { return; }
            
            base.ProcessAbility();

            if (_isCasting)
            {
                _castTimer += Time.fixedDeltaTime;
                
                if (_castTimer >= ColliderEnableTime && !_isAttackExecuted) {
                    _isAttackExecuted = true;
                    EnableHitCollider();
                }

                // Set animation trigger during ability, so there is no break at the end, if we chain attacks
                if (!_isAnimTriggerSet)
                {
                    // Set anim trigger of first/second basic attack
                    if (_castTimer >= _inputCacheThresholdMin && _inputCached) {
                        _isFirstAttackAnim = !_isFirstAttackAnim;
                        if (_isFirstAttackAnim) {
                            _netAnimator.SetTrigger(_castAnimationParameter);
                        }
                        else {
                            _netAnimator.SetTrigger(_castAnimationParameter2);
                        }

                        _isAnimTriggerSet = true;
                    }
                }
                
                // Stop attack state and reset ability
                if (_castTimer >= AttackTime) {
                    CastStop();
                }
            }
        }

        private void EnableHitCollider()
        {
            // Check for collisions and do damage to enemy players
            Vector3 pos = transform.position + transform.forward * _boxPosOffset.z + Vector3.up;
            var colls = Physics.OverlapBox(pos, _boxCollSize / 2, transform.rotation, _collisionLayerMask);

            foreach (var c in colls) {
                if (c.TryGetComponent(out Character player)) {
                    if (player.Net_TeamID.Value != _character.Net_TeamID.Value) {
                        player.LinkedHealth.Damage(Damage, this.gameObject);
                    }
                }
            }
        }

        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            _fxHandler.EnableWeaponTrailForTime(AttackTime);
            
            if (IsLocalPlayer)
            {
                _abilityHandler.StartCooldown(AbilityTypes.BasicAttack);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.BasicAttack);
            }
        }
        
        
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_castAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castAnimationParameter);
            RegisterAnimatorParameter(_castAnimationParameterName2, AnimatorControllerParameterType.Trigger, out _castAnimationParameter2);
            RegisterAnimatorParameter(_castEndAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castEndAnimationParameter);
        }

        private void OnDrawGizmos()
        {
            if (_isCasting && _castTimer >= ColliderEnableTime)
            {
                Vector3 pos = transform.position + transform.forward * _boxPosOffset.z + Vector3.up;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(pos, _boxCollSize);
                
            }
        }
    }
}