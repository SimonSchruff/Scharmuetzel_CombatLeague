using System.Collections;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class HealPotionAbility : CharacterAbility
    {
        [Header("Ability Settings")] 
        [SerializeField] private float _healAmount;
        [SerializeField] private float _timeToEnableLightFX = 0f;
        [SerializeField] private float _durationLightFX = 0f;
        [SerializeField] private float _totalAbilityTime = 0f;


        // [Header("Cooldown")]
        // public AbilityCooldown Cooldown;
        
        // Private vars
        private float _castTimer = 0f;
        private bool _isCasting = false;
        private bool _isFxEnabled= false;
        // Animation
        private const string _castAnimationParameterName = "DrinkPotion";
        private int _castAnimationParameter;
        
        /// <summary>
        /// On init we initialize our cooldown and feedback
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
            if (inputs.heal_input == KeyState.off || !IsServer) {
                return;
            }
            
            // Do not cast if has cooldown, or is not authorized because of movement/condition states
            if (!AbilityAuthorized || !Cooldown.Ready() || _isCasting) {
                return;
            }
        
            CastStart();
        }
        
        protected virtual void CastStart()
        {
            if (!Cooldown.Ready() || !IsServer) {
                return;
            }
        
            Cooldown.Start();

            if (IsServer) {
                NotifyAbilityStartedClientRPC();
            }
            
            _isCasting = true;
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);
            _netAnimator.SetTrigger(_castAnimationParameter);
        }
        
        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            StartCoroutine(WaitToEnablePotionLight());
            
            if (IsLocalPlayer)
            {
                _abilityHandler.StartCooldown(AbilityTypes.HealPotion);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.HealPotion);
            }
        }
        
        [ClientRpc]
        private void NotifyAbilityStoppedClientRPC(bool success)
        {
            _fxHandler.DisableHealPotionLight();

            if (success)
            {
                _fxHandler.PlayHealFX();
                _soundFXHandler.Play("HealEnd");
            }
        }

        private IEnumerator WaitToEnablePotionLight()
        {
            yield return new WaitForSeconds(_timeToEnableLightFX); 
            _fxHandler.EnableHealPotionLight();
            _soundFXHandler.Play("HealStart");

        }

        protected virtual void CastStop(bool success)
        {
            if (success) {
                _character.LinkedHealth.Heal(_healAmount);
            }
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            NotifyAbilityStoppedClientRPC(success);

            // Cooldown.Stop();
            _isFxEnabled = false;
            _isCasting = false;
            _castTimer = 0f;
        }
        
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_isCasting)
            {
                _castTimer += Time.fixedDeltaTime;

                if (_castTimer >= _totalAbilityTime) {
                    CastStop(true);
                }
                
            }
        }
        
        private void Update()
        {
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }

        private void OnDamageReceived(float newHealth)
        {
            InterruptAbility();
        }

        private void OnDeath(float respawnTime)
        {
            InterruptAbility();
        }

        private void InterruptAbility()
        {
            if (!_isCasting) {
                return;
            }
            
            CastStop(false);
        }
        
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_castAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castAnimationParameter);
        }
    }
}