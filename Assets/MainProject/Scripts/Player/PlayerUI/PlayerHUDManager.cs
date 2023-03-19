using System;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Tools.NetworkAnalysisTools;
using MainProject.Scripts.Tools.Services;
using UnityEngine;

namespace MainProject.Scripts.Player.PlayerUI
{
    public class PlayerHUDManager : MonoBehaviour
    {
        public static PlayerHUDManager Instance;
        
        [Header("References")]
        [SerializeField] private PlayerCanvas _playerCanvas;
        [SerializeField] private HealthBar _healthBarPrefab;
        
        [Header("MiniMap")]
        [SerializeField] private Transform _mapPoint_1;
        [SerializeField] private Transform _mapPoint_2;
        
        
        private NetStatsMonitorCustomization _netStatsMonitorHandler;
        private Character _localPlayerCharacter;
        private Health _health;
        private MiniMap _miniMap;
        
        private Dictionary<Character, HealthBar> _healthBarDictionary = new Dictionary<Character, HealthBar>();

        private float _timer;
        private float _maxHealth;
        private float _currHealth;

        private float _respawnTime;
        private string _lobbyCode;
        private float _pointsToWin;
        
        private bool _isMenuActive = false;
        private bool _isInitalized = false;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            } 
            else {
                Destroy(this);
            }

            _miniMap = _playerCanvas.GetComponentInChildren<MiniMap>();
            
            if (_miniMap != null && _mapPoint_1 != null && _mapPoint_2 != null)
            {
                _miniMap.CalculateMapRatio(_mapPoint_1, _mapPoint_2);
            }

            Character.OnCharacterSpawned += OnPlayerSpawnCallback;
            Character.OnCharacterDespawned += OnPlayerDespawnCallback;
            
            MatchmakingService.OnLobbyCreated += OnSetLobbyCodeUI;
        }
        
        private void OnDisable()
        {
            Character.OnCharacterSpawned -= OnPlayerSpawnCallback;
            Character.OnCharacterDespawned -= OnPlayerDespawnCallback;
            
            MatchmakingService.OnLobbyCreated += OnSetLobbyCodeUI;

            if (_localPlayerCharacter != null)
            {
                _health.Net_CurrentHealth.OnValueChanged -= DamageTaken;
                _health.OnDeath -= OnDeath;
                _health.OnRespawn -= OnRespawn;
                _localPlayerCharacter.LinkedInputHandler.OnStartMenuPressed -= ToggleMenu;
                GameManager.Instance.Net_TeamPoints.OnValueChanged -= OnTeamPointsChanged;
                GameManager.Instance.Net_FlagGameState.OnValueChanged -= OnFlagStateChanged;
            }
            
        }

        private void OnPlayerSpawnCallback(Character character)
        {
            // Set up health bar and save it to dictionary if it does not exist already
            if (!_healthBarDictionary.ContainsKey(character))
            {
                var healthBar = Instantiate(_healthBarPrefab, _playerCanvas.transform);
                healthBar.InitalizeHealthBar(character);
                _healthBarDictionary.Add(character, healthBar);
            }

            // Set up the reference for the local player character
            if (character.IsLocalPlayer)
            {
                _localPlayerCharacter = character;
            }
            
            _miniMap.UpdatePlayers(_healthBarDictionary);
        }

        /// <summary>
        /// Sets the name of the character to be visible on the health bar;
        /// Returns if character is not contained in healthBar dictionary;
        /// Called in player manager;
        /// </summary>
        public void SetPlayerNameOnHealthBar(Character character)
        {
            if (!_healthBarDictionary.ContainsKey(character)) {
                return;
            }

            var bar = _healthBarDictionary[character];
            bar.SetPlayerName(character.Net_PlayerName.Value);
        }

        private void OnSetLobbyCodeUI(string lobbyCode)
        {
            _lobbyCode = lobbyCode;
            _playerCanvas.SetLobbyCodeText(_lobbyCode);
        }

        private void OnPlayerDespawnCallback(Character character)
        {
            // Destroy healthBar and remove it from dictionary if player despawns
            if (_healthBarDictionary.ContainsKey(character))
            {
                var healthBar = _healthBarDictionary[character];
                if (healthBar.gameObject != null) {
                    Destroy(healthBar.gameObject);
                }
                
                _healthBarDictionary.Remove(character);
            }
            
            _miniMap.UpdatePlayers(_healthBarDictionary);
        }

        
        public void InitalizeLocalPlayer()
        {
            if (_localPlayerCharacter == null) {
                return;
            }

            // Components
            _health = _localPlayerCharacter.LinkedHealth;
            _netStatsMonitorHandler = FindObjectOfType<NetStatsMonitorCustomization>();
            
            _playerCanvas.GetComponent<Canvas>().enabled = true;
            _playerCanvas.Init();
           
            if (!String.IsNullOrEmpty(_lobbyCode)) {
                _playerCanvas.SetLobbyCodeText(_lobbyCode);
            }

            // Health
            _maxHealth = _health.MaxHealth;
            _playerCanvas.SetHealthSlider(1f, false);

            // Points
            _pointsToWin = GameManager.Instance.PointsToWin;
            _playerCanvas.SetPointSlider(1, 0);
            _playerCanvas.SetPointSlider(2, 0);
            
            // Lobby Code
            var lobby = MatchmakingService.CurrentLobby;
            if (MatchmakingService.CurrentLobby != null) {
                OnSetLobbyCodeUI(lobby.LobbyCode);
            }
            
            _playerCanvas.InitMenuHUDInfo(lobby.LobbyCode,lobby.Name );
            
            //Events
            _health.Net_CurrentHealth.OnValueChanged += DamageTaken;
            _health.OnRespawn += OnRespawn;
            _health.OnDeath += OnDeath;

            _localPlayerCharacter.LinkedInputHandler.OnStartMenuPressed += ToggleMenu;
            
            GameManager.Instance.Net_TeamPoints.OnValueChanged += OnTeamPointsChanged;
            GameManager.Instance.Net_FlagGameState.OnValueChanged += OnFlagStateChanged;
            
            _isInitalized = true;
        }


        private void LateUpdate()
        {
            if (!_isInitalized) {
                return;
            }

            if (_respawnTime > 0f)
            {
                _playerCanvas.SetRespawnTimerText(_respawnTime);
                _respawnTime -= Time.deltaTime;
            }

            if (_miniMap != null) {
                _miniMap.UpdatePlayerPos();
            }
        }

        public void SetCooldownTimeForAbility(AbilityTypes ability, float time)
        {
            _playerCanvas.SetCooldownTime(ability, time);
        }

        private void DamageTaken(float previous, float current)
        {
            _playerCanvas.SetHealthSlider(current / _maxHealth, previous > current);
            _playerCanvas.SetHealthBarText(current, _maxHealth);
        }

        public void ToggleNetStats()
        {
            if (_netStatsMonitorHandler == null) {
                _netStatsMonitorHandler = FindObjectOfType<NetStatsMonitorCustomization>();
            }
            
            _netStatsMonitorHandler?.ToggleMonitor();
        }

        private void ToggleMenu()
        {
            _isMenuActive = !_isMenuActive;
            print($"Toggle Menu {_isMenuActive}");
            
            if (_isMenuActive) {
                _playerCanvas.EnableMenuHUD();
            }
            else {
                _playerCanvas.DisableMenuHUD();
            }
        }

        

        public void TriggerAbilityCooldown(AbilityTypes ability)
        {
            _playerCanvas.StartAbilityCooldown(ability);
        }

        private void OnTeamPointsChanged(TeamPoints previous, TeamPoints current)
        {
            _playerCanvas.SetPointSlider(1, current.Points01 / _pointsToWin);
            _playerCanvas.SetPointSlider(2, current.Points02 / _pointsToWin);
        }

        private void OnFlagStateChanged(FlagGameState previous, FlagGameState current)
        {
            _playerCanvas.UpdateFlagImages(1, current.FlagState01);
            _playerCanvas.UpdateFlagImages(2, current.FlagState02);
            _playerCanvas.UpdateFlagImages(3, current.FlagState03);
        }


        private void OnDeath(float respawnTime)
        {
            _respawnTime = respawnTime;
            _playerCanvas.EnableRespawnTimerText();
            _playerCanvas.SetAbilityImagesByValue(0f);
            
            _localPlayerCharacter.LinkedCameraHandler.SetBW(true);
        }
        
        private void OnRespawn()
        {
            _respawnTime = 0f;
            _playerCanvas.DisableRespawnTimerText();
            _playerCanvas.SetAbilityImagesByValue(1f);
            
            _playerCanvas.ResetHealthSlider();
            
            _localPlayerCharacter.LinkedCameraHandler.SetBW(false);
        }
        
    }
}