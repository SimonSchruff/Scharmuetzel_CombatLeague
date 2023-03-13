using System;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class BasicRanged : CharacterAbility
    {
        [Header("Projectile")] 
        [SerializeField] protected OneDirectionProjectile ProjectilePrefab;

        [Header("Sprites")]
        [SerializeField] private Image AimDisplaySprite;
        
        [Header("Ability Settings")] 
        [Tooltip("Offset in local space of player for spawning projectile")]
        [SerializeField] private Vector3 SpawnOffset;
        [SerializeField] private float TimeToSpawnProjectile = 0.5f;
        [SerializeField] private float TimeToEndAbility = 1f;
        [Space(10)]
        [SerializeField] private float ProjectileLifeTime = 2f;
        [SerializeField] private float ProjectileMoveSpeed = 2f;
        [SerializeField] private DamageType Damage;

        // [Header("Cooldown")] 
        // public AbilityCooldown Cooldown;
        
        // Animation
        protected const string _castAnimationParameterName = "Cast01";
        protected int _castAnimationParameter;

        private float _castTimer;
        protected bool _isCasting;
        private bool _isProjectileSpawned;


        /// <summary>
        ///     On init we initialize our cooldown and feedback
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();
            Cooldown.Initialization();

            Assert.IsNotNull(ProjectilePrefab);
        }

        protected override void HandleInput(PlayerInputs inputs)
        {
            // Start dash if server and has input and not already dashing
            if (!IsServer)
            {
                return;
            }

            // Do not dash if has cooldown, or is not authorized because of movement/condition states
            if (!AbilityAuthorized || !Cooldown.Ready() || _isCasting)
            {
                return;
            }
            
            if (inputs.cast_01_input == KeyState.release)
            {
                CastStart();
            }
        }

        public bool AimAbility(PlayerInputs inputs)
        {
            if (!IsLocalPlayer) { return false; }
            
            if (!AbilityAuthorized || !_abilityHandler.CheckIfAbilityIsReady(AbilityTypes.GroundSpikes) || _isCasting) { return false; }

            if (inputs.cast_01_input == KeyState.press || inputs.cast_01_input == KeyState.held) {
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
            if (!Cooldown.Ready()) { return; }

            Cooldown.Start();
            NotifyAbilityStartedClientRPC();
            
            _isCasting = true;
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);

            _netAnimator.SetTrigger(_castAnimationParameter);
        }

        protected virtual void CastStop()
        {
            // Cooldown.Stop();
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
            _isCasting = false;
            _isProjectileSpawned = false;
            _castTimer = 0f;
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
                if (_castTimer >= TimeToSpawnProjectile && !_isProjectileSpawned) {
                    SpawnProjectiles();
                }

                if (_castTimer >= TimeToEndAbility) {
                    CastStop();
                }
            }
        }

        private void SpawnProjectiles()
        {
            _isProjectileSpawned = true;
            
            // Instantiate Projectile
            var spawnPos = transform.position + (transform.up * SpawnOffset.y) + (transform.forward * SpawnOffset.z) + (transform.right * SpawnOffset.x);
            var projectile = Instantiate(ProjectilePrefab);
            
            projectile.Initalize(_character.Net_TeamID.Value ,spawnPos, transform.forward, Damage, ProjectileMoveSpeed, ProjectileLifeTime);
            projectile.NetworkObject.Spawn(true);
        }
        
        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            StartCoroutine(PlaySoundAfterTime("BasicRanged", TimeToSpawnProjectile));
            
            if (IsLocalPlayer)
            {
                _abilityHandler.StartCooldown(AbilityTypes.BasicRanged);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.BasicRanged);
            }
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_castAnimationParameterName, AnimatorControllerParameterType.Trigger, out _castAnimationParameter);
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
    }
}