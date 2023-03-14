using System.Collections;
using System.Threading.Tasks;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class DragonPunchAbility : CharacterAbility
    {
        [Header("Ability Settings")]
        [SerializeField] private float _timeToCast;
        [SerializeField] private float _timeToMoveAfterCast = 3.5f;
        [SerializeField] private LayerMask _collisionLayerMask;
        [SerializeField] private Image AimDisplaySprite;
        
        [Header("Damage")]
        [SerializeField] private DamageType Damage;
        
        // [Header("Cooldown")] 
        // public AbilityCooldown Cooldown;
        
        // Animation
        private const string _castAnimationParameterName = "Cast04";
        private int _castAnimationParameter;
        
        private readonly Vector3 _boxCollSize = new Vector3(2f, 1f, 9.5f);
        private readonly Vector3 _boxPosOffset = new Vector3(0f,1f, 6f);
        
        private float _castTimer;
        private bool _isCasting = false;
        private bool _isAttackExecuted = false;
        
        /// <summary>
        /// On init we initialize our cooldown and feedback
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
            
            if (inputs.cast_04_input == KeyState.release) {
                CastStart();
            }
        }
        
        protected virtual void CastStart()
        {
            if (!Cooldown.Ready() || !IsServer) { return; }
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Casting);
            _netAnimator.SetTrigger(_castAnimationParameter);
            _isCasting = true;
            
            Cooldown.Start();
            NotifyAbilityStartedClientRPC();
        }

        public bool AimAbility(PlayerInputs inputs)
        {
            if (!IsLocalPlayer) { return false; }
            
            if (!AbilityAuthorized || !_abilityHandler.CheckIfAbilityIsReady(AbilityTypes.DragonPunch) || _isCasting) { return false; }

            if (inputs.cast_04_input == KeyState.press || inputs.cast_04_input == KeyState.held)
            {
                EnableHitAreaPreview();
                return true;
            }
            else
            {
                DisableHitAreaPreview();
                return false;
            }
        }
        
        public void DisableAimAbility()
        {
            if (!IsLocalPlayer) { return; }
            
            DisableHitAreaPreview();
        }

        private async void CastStop()
        {
            if (!IsServer) { return; }
            
            NotifyAbilityStopClientRPC();

            StartCoroutine(WaitToEnableMovement(_timeToMoveAfterCast)); 
            
            await Task.Delay(300);
            
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

            _isAttackExecuted = false;
            _isCasting = false;
            _castTimer = 0f;
        }
        
        [ClientRpc]
        private void NotifyAbilityStopClientRPC()
        {
            DisableHitAreaPreview();
            _fxHandler.PlayDragonPunchFX();

            if (IsLocalPlayer)
            {
                StartCoroutine(WaitToEnableMovement(_timeToMoveAfterCast));
            }
        }
        
        [ClientRpc]
        private void NotifyAbilityStartedClientRPC()
        {
            EnableHitAreaPreview();

            StartCoroutine(PlaySoundAfterTime("DragonPunch", _timeToCast - 0.1f));
            
            if (IsLocalPlayer) {
                _abilityHandler.StartCooldown(AbilityTypes.DragonPunch);
                PlayerHUDManager.Instance.TriggerAbilityCooldown(AbilityTypes.DragonPunch);
            }
        }
        
        private void Update()
        {
           // TODO: Cooldown.UpdateCooldown(IsHost) before; Was that a mistake or intended?
            Cooldown.UpdateCooldown(IsLocalPlayer);
        }
        
        public override void ProcessAbility()
        {
            if (!IsServer) { return; }
            
            base.ProcessAbility();

            if (_isCasting)
            {
                _castTimer += Time.fixedDeltaTime;
                if (_castTimer >= _timeToCast && !_isAttackExecuted) {
                    _isAttackExecuted = true;
                    CastStop();
                }
            }
        }

        private IEnumerator WaitToEnableMovement(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            
            _movementStateMachine.ChangeState(CharacterStates.CharacterMovementStates.Idle);
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