using System;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Player.PlayerUI;
using Unity.Netcode;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class PlayerAbilityHandler : NetworkBehaviour
    {
        private Character _character;

        private BasicAttack _basicAttack;
        private BasicRanged _basicRanged;
        private PlayerDash _dash;
        private HealPotionAbility _heal;
        private AreaSpikeAttack _spikeAttack;
        private SpinAttack _spinAttack;
        private DragonPunchAbility _dragonPunch;
        
        private AbilityCooldown _dashCd;
        private AbilityCooldown _healCd;
        private AbilityCooldown _basicAttackCd;
        private AbilityCooldown _basicRangedCd;
        private AbilityCooldown _dragonPunchCd;
        private AbilityCooldown _groundSpikesCd;
        private AbilityCooldown _spinAttackCd;

        private bool _isInitalized;

        // private  _basicRangedCd;

        public void Init()
        {
            if (!IsLocalPlayer) {
                return;
            }

            _character = this.GetComponent<Character>();
            _basicAttack = this.GetComponent<BasicAttack>();
            _dash = this.GetComponent<PlayerDash>();
            _heal = this.GetComponent<HealPotionAbility>();
            _basicRanged = this.GetComponent<BasicRanged>();
            _spikeAttack = this.GetComponent<AreaSpikeAttack>();
            _spinAttack = this.GetComponent<SpinAttack>();
            _dragonPunch = this.GetComponent<DragonPunchAbility>();
            
            SetCooldownTimes();
            
            _isInitalized = true;
        }

        private void Update()
        {
            if (!_isInitalized || !_character.IsLocalPlayer) {
                return;
            }
            
            _basicAttackCd.UpdateCooldown(_character.IsLocalPlayer);
            _basicRangedCd.UpdateCooldown(_character.IsLocalPlayer);
            _groundSpikesCd.UpdateCooldown(_character.IsLocalPlayer);
            _dragonPunchCd.UpdateCooldown(_character.IsLocalPlayer);
            _spinAttackCd.UpdateCooldown(_character.IsLocalPlayer);
            _dashCd.UpdateCooldown(_character.IsLocalPlayer);
        }

        private void SetCooldownTimes()
        {
            if (!PlayerHUDManager.Instance || !IsLocalPlayer) { return;}
            
            _dashCd = _dash.Cooldown; 
            float dashCdTime = _dashCd.ConsumptionDuration + _dashCd.PauseOnEmptyDuration + _dashCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.Dash, dashCdTime);
            
            _basicAttackCd = _basicAttack.Cooldown; 
            float basicAttackCdTime = _basicAttackCd.ConsumptionDuration + _basicAttackCd.PauseOnEmptyDuration + _basicAttackCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.BasicAttack, basicAttackCdTime);
            
            _basicRangedCd = _basicRanged.Cooldown;
            float basicRangedCdTime = _basicRangedCd.ConsumptionDuration + _basicRangedCd.PauseOnEmptyDuration + _basicRangedCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.BasicRanged, basicRangedCdTime);
            
            _groundSpikesCd = _spikeAttack.Cooldown;
            float groundSpikesCdTime = _groundSpikesCd.ConsumptionDuration + _groundSpikesCd.PauseOnEmptyDuration + _groundSpikesCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.GroundSpikes, groundSpikesCdTime);
            
            _spinAttackCd = _spinAttack.Cooldown;
            float spinAttackCdTime = _spinAttackCd.ConsumptionDuration + _spinAttackCd.PauseOnEmptyDuration + _spinAttackCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.SpinAttack, spinAttackCdTime);
            
            _dragonPunchCd = _dragonPunch.Cooldown;
            float dragonPunchCdTime = _dragonPunchCd.ConsumptionDuration + _dragonPunchCd.PauseOnEmptyDuration + _dragonPunchCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.DragonPunch, dragonPunchCdTime);

            _healCd = _heal.Cooldown;
            float healCdTime = _healCd.ConsumptionDuration + _healCd.PauseOnEmptyDuration + _healCd.RefillDuration;
            PlayerHUDManager.Instance.SetCooldownTimeForAbility(AbilityTypes.HealPotion, healCdTime);
        }

        public void HandleAimForAbilities(PlayerInputs inputs)
        {
            if (!_isInitalized ) {
                return;
            }
            
            // Reset all aims
            _spikeAttack.DisableAimAbility();
            _dragonPunch.DisableAimAbility();   
            _spinAttack.DisableAimAbility();
            _basicRanged.DisableAimAbility();

            if (_dragonPunch.AimAbility(inputs)) {
                return;
            }
            
            if (_spikeAttack.AimAbility(inputs)) {
                return;
            }
            
            if (_spinAttack.AimAbility(inputs)) {
                return;
            }
            
            if (_basicRanged.AimAbility(inputs)) {
                return;
            }
        }

        /// <summary>
        /// Start the local cooldown of an ability; Used for prediction;
        /// </summary>
        /// <param name="ability"></param>
        public void StartCooldown(AbilityTypes ability)
        {
            if (!_isInitalized || !IsLocalPlayer) {
                return;
            }
            
            switch (ability)
            {
                case AbilityTypes.Dash:
                    _dashCd.Start();
                    break;
                case AbilityTypes.BasicAttack:
                    _basicAttackCd.Start();
                    break;
                case AbilityTypes.BasicRanged:
                    _basicRangedCd.Start();
                    break;
                case AbilityTypes.GroundSpikes:
                    _groundSpikesCd.Start();
                    break;
                case AbilityTypes.SpinAttack:
                    _spinAttackCd.Start();
                    break;
                case AbilityTypes.DragonPunch:
                    _dragonPunchCd.Start();
                    break;
            }
        }

        /// <summary>
        /// Returns true if the ability is ready;
        /// This is only a prediction on the local client; Actual state of ability might differ by rtt!
        /// </summary>
        public bool CheckIfAbilityIsReady(AbilityTypes ability)
        {
            switch (ability)
            {
                case AbilityTypes.Dash:
                    return _dashCd.Ready();
                    break;
                case AbilityTypes.BasicAttack:
                    return _basicAttackCd.Ready();
                    break;
                case AbilityTypes.BasicRanged:
                    return _basicRangedCd.Ready();
                    break;
                case AbilityTypes.GroundSpikes:
                    return _groundSpikesCd.Ready();
                    break;
                case AbilityTypes.SpinAttack:
                    return _spinAttackCd.Ready();
                    break;
                case AbilityTypes.DragonPunch:
                    return _dragonPunchCd.Ready();
                    break;
                default:
                    return false;
                    break;
            }
        }

        public void ResetAbilityCooldowns()
        {
            _dashCd.SetCooldownReady();
            _healCd.SetCooldownReady();
            _basicAttackCd.SetCooldownReady();
            _basicRangedCd.SetCooldownReady();
            _dragonPunchCd.SetCooldownReady();
            _groundSpikesCd.SetCooldownReady();
            _spinAttackCd.SetCooldownReady();
        }


    }
    
    
}