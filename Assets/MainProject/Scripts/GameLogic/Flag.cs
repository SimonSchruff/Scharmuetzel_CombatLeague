using System;
using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player;
using MainProject.Scripts.Tools;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


namespace MainProject.Scripts.GameLogic
{
    public class Flag : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int flag_index;

        private NetworkVariable<int> Net_CurrentPoints = new NetworkVariable<int>();
        public FlagState CurrentFlagState => _currentFlagState;
        private int Team01Players => Team01PlayerList.Count;
        private int Team02Players => Team02PlayerList.Count;

        private List<Character> Team01PlayerList = new List<Character>();
        private List<Character> Team02PlayerList = new List<Character>();

        [Header("State")]
        [SerializeField] private FlagState _currentFlagState;
        [SerializeField] private FlagState _previousFlagState;
        
        [Tooltip("To points it takes to capture a flag with 50points/second;")]
        [SerializeField] private  int points_to_capture = 500;
        
        [SerializeField] private int current_points;
        private const int points_per_tick = 1;

        [Header("Visual References")]
        [SerializeField] private Slider _pointSlider;
        [SerializeField] private Image _fillImg;
        [SerializeField] private GameObject _fireYellow; 
        [SerializeField] private GameObject _fireBlue;
        [SerializeField] private GameObject _fireRed;
        [SerializeField] private MeshRenderer _cylinderMR;
        
        [Header("Color Settings")]
        [SerializeField] private Color _team01OwnedColor;
        [SerializeField] private Color _team01ChargingColor;
        [SerializeField] private Color _team02OwnedColor;
        [SerializeField] private Color _team02ChargingColor;
        [SerializeField] private Color _neutralColor;

        private AudioSource _audioSource;
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) {
                _currentFlagState = FlagState.Neutral;
                NetworkManager.NetworkTickSystem.Tick += Tick;
                FlagStateChanged(FlagState.Neutral);
            }
            else {
                Net_CurrentPoints.OnValueChanged += OnPointsChanged;
            }
            
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) {
                NetworkManager.NetworkTickSystem.Tick -= Tick;
            }
            else {
                Net_CurrentPoints.OnValueChanged += OnPointsChanged;
            }
            
        }

        private void Tick()
        {
            if (!IsServer) {
                return;
            }

            switch (CurrentFlagState)
            {
                case FlagState.Team01Charging:
                    current_points += points_per_tick;
                    if (current_points >= points_to_capture) {
                        FlagStateChanged(FlagState.Team01Owned);
                    }
                    break;
                case FlagState.Team02Charging:
                    current_points += points_per_tick;
                    if (current_points >= points_to_capture) {
                        FlagStateChanged(FlagState.Team02Owned);
                    }
                    break;
                case FlagState.Team01Decharging:
                    current_points -= points_per_tick;
                    if (current_points <= 0) {
                        FlagStateChanged(FlagState.Team02Charging);
                        current_points = 0;
                    }
                    break;
                case FlagState.Team02Decharging:
                    current_points -= points_per_tick;
                    if (current_points <= 0) {
                        FlagStateChanged(FlagState.Team01Charging);
                        current_points = 0;
                    }
                    break;
                case FlagState.Team01Owned:
                    GameManager.Instance.AddTeamPoints(1);
                    break;
                case FlagState.Team02Owned:
                    GameManager.Instance.AddTeamPoints(2);
                    break;
                default:
                    break;
            }

            // Set Network points
            if (Net_CurrentPoints.Value != current_points) {
                Net_CurrentPoints.Value = current_points;
            }
            
            _pointSlider.value = (float)current_points / (float)points_to_capture;
        }
        
        private void OnTriggerEnter(Collider c)
        {
            if (!IsServer) { return; }
            
            // Check if player
            var playerCharacter = c.GetComponent<Character>(); 
            if (playerCharacter == null) {
                return;
            }
            
            if (playerCharacter.Net_TeamID.Value == 1) {
                Team01PlayerList.Add(playerCharacter);
            }
            
            if (playerCharacter.Net_TeamID.Value == 2) {
                Team02PlayerList.Add(playerCharacter);
            }

            // Subscribe to on death event, to be able to remove player from list if he/she dies
            playerCharacter.LinkedHealth.OnDeath += respawnTime => OnPlayerDeath(respawnTime, playerCharacter);
            
            Evaluate_Player_Amount();
        }

        private void OnTriggerExit(Collider c)
        {
            if (!IsServer) { return;}
            
            // Check if player
            var playerCharacter = c.GetComponent<Character>(); 
            if (playerCharacter == null) {
                return;
            }
            
            playerCharacter.LinkedHealth.OnDeath -= respawnTime => OnPlayerDeath(respawnTime, playerCharacter);
            
            if (playerCharacter.Net_TeamID.Value == 1) {
                Team01PlayerList.Remove(playerCharacter);
            }
            
            if (playerCharacter.Net_TeamID.Value == 2) {
                Team02PlayerList.Remove(playerCharacter);
            }
            
            Evaluate_Player_Amount();
        }

        private void OnPlayerDeath(float respawnTime, Character character)
        {
            if (Team01PlayerList.Contains(character)) {
                Team01PlayerList.Remove(character);
            }
            
            if (Team02PlayerList.Contains(character)) {
                Team02PlayerList.Remove(character);
            }
            
            Evaluate_Player_Amount();
        }

        private void OnPointsChanged(int previous, int current)
        {
            if (IsServer) { return; }
            
            _pointSlider.value = (float)current / (float)points_to_capture;
        }
        
        /// <summary>
        /// Every time player amount changes decide on the new state of the flag depending on current and previous flag state;
        /// </summary>
        private void Evaluate_Player_Amount()
        {
            FlagState newState = _currentFlagState;
            
            // Flag state at beginning -> Neutral
            if (_currentFlagState == FlagState.Neutral /* || _previousFlagState == FlagState.None  */ )
            {
                newState = HandleNeutralStates();
            }

            // Handle Paused flag State
            if (_currentFlagState == FlagState.Paused) 
            {
                // Neutral previous states
                if (_previousFlagState == FlagState.Neutral  || _previousFlagState == FlagState.Paused ) {
                    newState = HandleNeutralStates();
                }
                
                // Team01 previous states
                if (_previousFlagState == FlagState.Team01Charging || _previousFlagState == FlagState.Team01Decharging || _previousFlagState == FlagState.Team01Owned) {
                    newState = HandleTeam01States();
                }
                
                // Team02 previous states
                if (_previousFlagState == FlagState.Team02Charging || _previousFlagState == FlagState.Team02Decharging || _previousFlagState == FlagState.Team02Owned) {
                    newState = HandleTeam02States();
                }
            }
            
            // Handle states if team 01 is currently capturing 
            if (_currentFlagState == FlagState.Team01Charging || _currentFlagState == FlagState.Team01Decharging || _currentFlagState == FlagState.Team01Owned)
            {
                newState = HandleTeam01States();
            }
            
            // Handle states if team 02 is currently capturing 
            if (_currentFlagState == FlagState.Team02Charging || _currentFlagState == FlagState.Team02Decharging || _currentFlagState == FlagState.Team02Owned)
            {
                newState = HandleTeam02States();
            }
            
            FlagStateChanged(newState);
        }

        private FlagState HandleNeutralStates()
        {
            FlagState newState = _currentFlagState;
            
            // No members of each team
            if (Team01Players == 0 &&  Team02Players == 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Neutral;
            }
            
            // At least 1 member of each team
            if (Team01Players > 0 &&  Team02Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Neutral;
            }
                
            // No Team02 and more then one Team01
            if (Team01Players > 0 && Team02Players == 0)
            {
                // Already owned by team 01
                _previousFlagState = _currentFlagState;
                newState = FlagState.Team01Charging;
            }
                
            // No owner and more then 1 challenger
            if (Team01Players == 0 && Team02Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Team02Charging;
            }
            
            return newState;
        }   
        
        private FlagState HandleTeam01States()
        {
            FlagState newState = _currentFlagState;
            
            // No members of each team
            if (Team01Players == 0 &&  Team02Players == 0)
            {
                _previousFlagState = _currentFlagState;
                
                // Already owned by team 01
                if (_currentFlagState == FlagState.Team01Owned) {
                    return newState;
                }
                
                newState = FlagState.Paused;
            }
            
            // At least 1 member of each team
            if (Team01Players > 0 &&  Team02Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Paused;
            }
                
            // No Team02 and more then one Team01
            if (Team01Players > 0 && Team02Players == 0)
            {
                _previousFlagState = _currentFlagState;
                
                // Already owned by team 01
                if (_currentFlagState == FlagState.Team01Owned) {
                    return newState;
                }

                newState = FlagState.Team01Charging;
            }
                
            // No owner and more then 1 challenger
            if (Team01Players == 0 && Team02Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Team01Decharging;
            }

            return newState;
        }
        
        private FlagState HandleTeam02States()
        {
            FlagState newState = _currentFlagState;
            
            // No members of each team
            if (Team01Players == 0 &&  Team02Players == 0)
            {
                _previousFlagState = _currentFlagState;
                // Already owned by team 02
                if (_currentFlagState == FlagState.Team02Owned) {
                    return newState;
                }

                newState = FlagState.Paused;
            }
            
            // At least 1 member of each team
            if (Team02Players > 0 &&  Team01Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Paused;
            }
                
            // No Team02 and more then one Team01
            if (Team02Players > 0 && Team01Players == 0)
            {
                _previousFlagState = _currentFlagState;
                // Already owned by team 02
                if (_currentFlagState == FlagState.Team02Owned) {
                    return newState;
                }

                newState = FlagState.Team02Charging;
            }
                
            // No owner and more then 1 challenger
            if (Team02Players == 0 && Team01Players > 0)
            {
                _previousFlagState = _currentFlagState;
                newState = FlagState.Team02Decharging;
            }

            return newState;
        }

        
        [ClientRpc]
        private void OnFlagStateChangedClientRpc(FlagState newState)
        {
            if (IsServer) {
                return;
            }
            
            FlagStateChanged(newState);
        }
        
        private void FlagStateChanged(FlagState newState)
        {
            if (newState == _currentFlagState) {
                return;
            }

            _previousFlagState = _currentFlagState;
            _currentFlagState = newState;
            
            // Send to game manager and other clients if server
            if (IsServer) {
                OnFlagStateChangedClientRpc(newState);
                GameManager.Instance.OnChangeFlagGameState(flag_index, newState);
            }
            
            // Change visuals according to new state
            switch (newState)
            {
                case FlagState.Neutral:
                //case FlagState.None:
                    _fillImg.color = _neutralColor;
                    _cylinderMR.material.color = _neutralColor;
                    _fireYellow.SetActive(true);
                    _fireBlue.SetActive(false);
                    _fireRed.SetActive(false);
                    break;
                case FlagState.Paused:
                    _cylinderMR.material.color = _neutralColor;
                    _fireYellow.SetActive(true);
                    _fireBlue.SetActive(false);
                    _fireRed.SetActive(false);
                    break;
                case FlagState.Team01Owned:
                    _cylinderMR.material.color = _team01OwnedColor;
                    _fillImg.color = _team01OwnedColor;
                    _fireYellow.SetActive(false);
                    _fireBlue.SetActive(false);
                    _fireRed.SetActive(true);
                    _audioSource.Play();
                    break;
                case FlagState.Team01Charging:
                case FlagState.Team01Decharging:
                    _cylinderMR.material.color = _team01ChargingColor;
                    _fillImg.color = _team01ChargingColor;
                    _fireYellow.SetActive(false);
                    _fireBlue.SetActive(false);
                    _fireRed.SetActive(true);
                    break;
                case FlagState.Team02Decharging:
                case FlagState.Team02Charging:
                    _cylinderMR.material.color = _team02ChargingColor;
                    _fillImg.color = _team02ChargingColor;
                    _fireYellow.SetActive(false);
                    _fireBlue.SetActive(true);
                    _fireRed.SetActive(false);
                    break;
                case FlagState.Team02Owned:
                    _cylinderMR.material.color = _team02OwnedColor;
                    _fillImg.color = _team02OwnedColor;
                    _fireYellow.SetActive(false);
                    _fireBlue.SetActive(true);
                    _fireRed.SetActive(false);
                    _audioSource.Play();
                    break;
            }

        }
    }
}

