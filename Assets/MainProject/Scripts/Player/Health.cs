using System;
using System.Collections;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player.CharacterAbilities;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class Health : NetworkBehaviour
    {
        // PUBLIC VARS
        [Tooltip("The current health of the character synced over the network;")]
        public NetworkVariable<float> Net_CurrentHealth = new NetworkVariable<float>();

        [Header("References")] 
        public HealthBar HealthBar;
        
        [Header("Settings")]
        [Tooltip("The current health of the character;")]
        public float MaxHealth;

        [Tooltip("If this is true, obj cannot take any damage currently;")]
        public bool Invulnerable { get; private set; }


        // EVENTS
        // On hit/take dmg event
        public event Action<DamageType> OnDamageTaken;
        public event Action<float> OnDeath;
        public event Action OnRespawn;
        
        // COMPONENTS
        private Character _character;
        private PlayerMovement _characterMovement;
        private NetworkAnimator _netAnimator;
        private CharacterController _characterController;
        private TopDownController _controller;
        
        // ANIMATION
        private const string _onHitAnimationParameterName = "OnHit";
        private const string _respawnAnimationParameterName = "Respawn";
        private const string _deathAnimationParameterName = "Death";

        private int _onHitAnimationParameter;
        private int _respawnAnimationParameter;
        private int _deathAnimationParameter;

        // PRIVATE VARS
        private bool _isInitalized = false;

        public override void OnNetworkSpawn()
        {
            _character = this.gameObject.GetComponent<Character>();
            _controller = this.gameObject.GetComponent<TopDownController>();
            _characterController = this.gameObject.GetComponent<CharacterController>();
            _netAnimator = this.gameObject.GetComponent<NetworkAnimator>();
            _characterMovement = _character.FindAbility<PlayerMovement>();
            
            RegisterAnimatorParameter(_onHitAnimationParameterName, out _onHitAnimationParameter);
            RegisterAnimatorParameter(_respawnAnimationParameterName, out _respawnAnimationParameter);
            RegisterAnimatorParameter(_deathAnimationParameterName, out _deathAnimationParameter);
            
            if (IsServer) {
                SetHealth(MaxHealth);
            }

            _isInitalized = true;
        }

        public override void OnNetworkDespawn()
        {
        }

        /// <summary>
        /// Sets the current health to the specified new value, and updates the health bar
        /// </summary>
        /// <param name="newValue"></param>
        public virtual void SetHealth(float newValue)
        {
            if (!IsServer)
            {
                Debug.LogWarning("Only Server is allowed to set health!");
                return;
            }

            Net_CurrentHealth.Value = newValue;
            // UpdateHealthBar(false);
            // HealthChangeEvent.Trigger(this, newValue);
        }

        /// <summary>
        /// Returns true if this Health component can be damaged this frame, and false otherwise
        /// </summary>
        /// <returns></returns>
        public virtual bool CanTakeDamageThisFrame()
        {
            // if the object is invulnerable, we do nothing and exit
            if (Invulnerable) {
                return false;
            }

            if (!this.enabled) {
                return false;
            }

            // if we're already below zero, we do nothing and exit
            if ((Net_CurrentHealth.Value <= 0) && (MaxHealth != 0)) {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Returns true if this Health component can be damaged this frame, and false otherwise
        /// </summary>
        /// <returns></returns>
        public virtual bool CanHealThisFrame()
        {
            // if the object is invulnerable, we do nothing and exit
            if (Invulnerable) {
                return false;
            }

            if (!this.enabled) {
                return false;
            }

            // if we're already at max health, we do nothing and exit
            if ((Net_CurrentHealth.Value >= MaxHealth) && (MaxHealth != 0)) {
                return false;
            }

            return true;
        }

        public void Heal(float amount)
        {
            if (!IsServer) { Debug.LogWarning("Only Server is allowed to heal!"); return; }
            
            if (!CanHealThisFrame()) {
                return;
            }

            float newHealthAmount = amount + Net_CurrentHealth.Value;
            if (newHealthAmount >= MaxHealth) {
                SetHealth(MaxHealth);
            }
            else {
                SetHealth(newHealthAmount);
            }
        }

        #region DAMAGE_RECEIVED
        public virtual void Damage(DamageType damage, GameObject instigator, float invincibilityDuration = 0.0f)
        {
            if (!IsServer) { Debug.LogWarning("Only Server is allowed to deal damage!"); return; }

            // Character is invulnerable currently return
            if (!CanTakeDamageThisFrame()) {
                return;
            }

            // Apply damage to health
            if (damage.DamageCaused > 0) {
                SetHealth(Net_CurrentHealth.Value - damage.DamageCaused);
            }
            
            // Kill character if dmg is equal or smaller 0
            if (Net_CurrentHealth.Value <= 0) {
                Die();
                return;
            }

            // we prevent the character from colliding with Projectiles, Player and Enemies
            if (invincibilityDuration > 0) {
                // Set invulnerable for amount of time
            }

            // Trigger damage taken event
            OnDamageTaken?.Invoke(damage);

            // Animation
            /*
            if (!damage.ForceCharacterCondition ) {
                _netAnimator.SetTrigger(_onHitAnimationParameter);
            }
            */
            
            // Apply damage player condition, if there is any
            ProcessDamageConditionStateChange(damage);

            // Apply damage movement multiplier, if there is any
            ProcessMovementMultiplierChange(damage);
        }

        private void ProcessDamageConditionStateChange(DamageType dmg)
        {
            if ((dmg == null) || (_character == null) || (!IsServer)) {
                return;
            }

            if (dmg.ForceCharacterCondition) {
                _character.ChangeCharacterConditionTemporarily(dmg.Condition, dmg.ConditionDuration);
            }
        }
        
        private void ProcessMovementMultiplierChange(DamageType dmg)
        {
            if ((dmg == null) || (_character == null) || (!IsServer)) {
                return;
            }

            if (dmg.ApplyMovementMultiplier) {
                ProcessDamageMovementMultiplierClientRpc(dmg.MovementMultiplier, dmg.MovementMultiplierDuration);
            }
        }
        #endregion
        
        [ClientRpc]
        private void ProcessDamageMovementMultiplierClientRpc(float movementMultiplier, float duration)
        {
            if (IsLocalPlayer || IsServer)
            {
                _characterMovement.ApplyMovementMultiplier(movementMultiplier, duration);
            }
        }
        

        private void Die()
        {
            if (!IsServer) { Debug.LogWarning("Only Server is allowed to kill player!"); return; }
            
            // Get time to respawn
            var respawnTime = GameManager.Instance.GetRespawnTime();
            
            // Set state of player
            SetHealth(0f);
            DamageDisabled();
            
            _character.ConditionStateMachine.ChangeState(CharacterStates.CharacterConditions.Dead);
            _character.MovementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.None);

            // Set animation
            _netAnimator.ResetTrigger(_onHitAnimationParameter);
            _netAnimator.SetTrigger(_deathAnimationParameter);

            StartCoroutine(StartRespawnCoroutine(respawnTime));
            
            // Send update to all clients
            DieClientRpc(respawnTime);
        }

        private IEnumerator StartRespawnCoroutine(float respawnTime)
        {
            if (!IsServer) {
                yield break;
            }
            
            yield return new WaitForSeconds(respawnTime);
            
            Respawn();
        }

        [ClientRpc]
        private void DieClientRpc(float respawnTime)
        {
            OnDeath?.Invoke(respawnTime);
        }

        private void Respawn()
        {
            if (!IsServer) {
                return;
            }
            
            PlayerManager.Instance.SetPlayerToSpawnPosition((int)_character.OwnerClientId);

            SetHealth(MaxHealth);
            DamageEnabled();
            
            _character.ConditionStateMachine.ChangeState(CharacterStates.CharacterConditions.Normal);
            _character.MovementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            
            _netAnimator.SetTrigger(_respawnAnimationParameter);
            
            RespawnClientRpc();
        }
        
        [ClientRpc]
        private void RespawnClientRpc()
        {
            OnRespawn?.Invoke();
        }

        protected virtual void RegisterAnimatorParameter(string parameterName, out int parameter)
        {
            parameter = Animator.StringToHash(parameterName);
        }

        #region INVULNERABILITY
        /// <summary>
        /// Prevents the character from taking any damage
        /// </summary>
        public virtual void DamageDisabled()
        {
            if (!IsServer) { Debug.LogWarning("Only Server is allowed to set invulnerability!"); return; }

            Invulnerable = true;
        }

        /// <summary>
        /// Allows the character to take damage
        /// </summary>
        public virtual void DamageEnabled()
        {
            if (!IsServer) { Debug.LogWarning("Only Server is allowed to set invulnerability!"); return; }

            Invulnerable = false;
        }
        #endregion

    }
}