using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using MainProject.Scripts.Tools.Services;
using MainProject.Scripts.DataStructures;

namespace MainProject.Scripts.Lobby
{
    /// <summary>
    ///     NetworkBehaviours cannot easily be parented, so the network logic will take place
    ///     on the network scene object "NetworkLobby"
    /// </summary>
    public class RoomScreen : MonoBehaviour {
        [Header("Player Panel")]
        [SerializeField] private LobbyPlayerPanel _playerPanelPrefab;
        [SerializeField] private Transform _playerPanel01, _playerPanel02, _playerPanel03;
        private List<LobbyPlayerPanel> _playerPanels = new List<LobbyPlayerPanel>();
        private List<GameObject> _displayPlayersGO = new List<GameObject>();
        
        [Header("UI Refs")]
        [SerializeField] private TMP_Text _waitingText;
        [SerializeField] private TMP_Text _lobbyCodeText, _mapText;
        [SerializeField] private GameObject _startButton, _readyButton, _waitButton;
        [SerializeField] private GameObject _bigMapImg, _smallMapImg;

        [Header("Display Player")]
        [SerializeField] private Transform[] _displayPlayerPositionsTeam01 = new Transform[3];
        [SerializeField] private Transform[] _displayPlayerPositionsTeam02 = new Transform[3];
        [SerializeField] private LobbyDisplayPlayer _displayPlayerTeam01, _displayPlayerTeam02;
        
        private bool _allReady;
        private bool _ready;
        private string _currentMap;
        private string _lobbyCode;

        private Dictionary<ulong, PlayerLobbyData> _currentPlayers = new();
        private int _team01Players, _team02Players, _team03Players;
        private int _playerRoomCapacity = 3;
        
        public static event Action StartPressed; 
        public static event Action<ulong, int> PlayerSwitchTeam; 


        private void OnEnable() {
            foreach (Transform child in _playerPanel01) Destroy(child.gameObject);
            foreach (Transform child in _playerPanel02) Destroy(child.gameObject);
            foreach (Transform child in _playerPanel03) Destroy(child.gameObject);
            _team01Players = 0;
            _team02Players = 0;
            _team03Players = 0;

            _playerPanels.Clear();

            LobbyHandler.LobbyPlayersUpdated += NetworkLobbyPlayersUpdated;
            MatchmakingService.CurrentLobbyRefreshed += OnCurrentLobbyRefreshed;
            
            _startButton.SetActive(false);
            _waitButton.SetActive(false);
            _readyButton.SetActive(false);
            
            UpdateLobbyData();

            _ready = false;
        }

        private void OnDisable() {
            LobbyHandler.LobbyPlayersUpdated -= NetworkLobbyPlayersUpdated;
            MatchmakingService.CurrentLobbyRefreshed -= OnCurrentLobbyRefreshed;
        }

        public static event Action LobbyLeft;

        public void OnLeaveLobby() {
            LobbyLeft?.Invoke();
        }

        public void UpdateLobbyData()
        {
            var lobby = MatchmakingService.CurrentLobby;
            if(lobby == null) return;
            
            _currentMap  = lobby.Data[Constants.CurrentMapName].Value;
            _lobbyCode  = lobby.LobbyCode;
            
            SetInfoSectionUI();
        }

        private void SetInfoSectionUI()
        {
            _lobbyCodeText.text = $"Lobby Code: {_lobbyCode}";
            _mapText.text = $"Map: {_currentMap}";
            
            if (_currentMap == "3v3") {
                _bigMapImg.SetActive(true);
                _smallMapImg.SetActive(false);
            }
            else {
                _bigMapImg.SetActive(false);
                _smallMapImg.SetActive(true);
            }
        }


        private void NetworkLobbyPlayersUpdated(Dictionary<ulong, PlayerLobbyData> players )
        {
            _currentPlayers = players;
            UpdateTeamCapacity();

            var playerAmount = (ulong) players.Count;

            // Remove all panels
            foreach (var panel in _playerPanels) { Destroy(panel.gameObject); }
            _playerPanels.Clear();

            // Remove all display players
            foreach (var p in _displayPlayersGO) { Destroy(p); }
            _displayPlayersGO.Clear();

            // Set up UI Player Panels
            for (ulong i = 0; i < (ulong) _currentPlayers.Count; i++)
            {
                // Create Player Panel
                var panel = Instantiate(_playerPanelPrefab, GetTeamParent(_currentPlayers[i].TeamID));
                panel.Init(_currentPlayers[i].ID, _currentPlayers[i].PlayerName, _currentPlayers[i].TeamID,
                    _currentPlayers[i].IsReady);

                if (_currentPlayers[i].IsReady)
                {
                    panel.SetReady();
                }
                else
                {
                    panel.SetPending();
                }

                _playerPanels.Add(panel);
            }


            UpdateDisplayPlayers(players);

            _startButton.SetActive(NetworkManager.Singleton.IsHost && players.All(p => p.Value.IsReady));
            _waitButton.SetActive(!NetworkManager.Singleton.IsServer && _ready);
            _readyButton.SetActive(!_ready);
        }

        private void UpdateDisplayPlayers(Dictionary<ulong, PlayerLobbyData> players)
        {
            // Set up Display Players for teams
            int team01PlayersSpawned = 0;
            int team02PlayersSpawned = 0;
            for(int i = 0; i < _currentPlayers.Count; i++)
            {
                if (!players.ContainsKey((ulong)i)) {
                    Debug.LogWarning("Not a valid player key" + i);
                }
                
                var player = players[(ulong)i];

                // Spawn Display Player
                switch (player.TeamID)
                {
                    case 1:
                        var displayPlayer = Instantiate(  _displayPlayerTeam01 , _displayPlayerPositionsTeam01[team01PlayersSpawned] );
                        displayPlayer.SetPlayerObjectActiveAfterTime(player.TeamID, 1f, player.PlayerName);
                        _displayPlayersGO.Add(displayPlayer.gameObject);
                        team01PlayersSpawned++;
                        break;
                    case 2:
                        var displayPlayer02 = Instantiate(  _displayPlayerTeam02 , _displayPlayerPositionsTeam02[team02PlayersSpawned] );
                        displayPlayer02.SetPlayerObjectActiveAfterTime(player.TeamID, 1f, player.PlayerName);
                        _displayPlayersGO.Add(displayPlayer02.gameObject);
                        team02PlayersSpawned++;
                        break;
                    default:
                        break;
                }
            }
        }

        private Transform GetTeamParent(int teamID)
        {
            switch (teamID)
            {
                case 1:
                    //if (_team01Players >= _playerRoomCapacity) { return GetTeamParentByCapacity(); }
                     { return _playerPanel01; }
                    break;
                case 2:
                    //if (_team02Players >= _playerRoomCapacity) { return GetTeamParentByCapacity(); }
                     { return _playerPanel02; }
                    break;
                case 3:
                    //if (_team03Players >= _playerRoomCapacity) { return GetTeamParentByCapacity(); }
                     { return _playerPanel03; }
                    break;
                default:
                    Debug.LogWarning($"Rooms are all full or index faulty {teamID}");
                    return null;
                    break;
            }
        }
        
        private Transform GetTeamParentByCapacity()
        {
            if(_team01Players < _playerRoomCapacity) {
                return _playerPanel01;
            }
            else if(_team02Players < _playerRoomCapacity) {
                return _playerPanel02;
            }
            else if(_team03Players < _playerRoomCapacity) {
                return _playerPanel03;
            }
            else {
                Debug.LogWarning($"Rooms are all full or index faulty");
                return null;
            }
        }

        private void OnCurrentLobbyRefreshed(Unity.Services.Lobbies.Models.Lobby lobby) {
            _waitingText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers} players in lobby...";
            UpdateTeamCapacity();
        }

        public void OnReadyClicked() {
            _ready = !_ready;
            _readyButton.SetActive(!_ready);
        }

        public void OnStartClicked() {
            StartPressed?.Invoke();
        }

        public void OnSwitchTeam(int newTeamID)
        {
            var clientId = NetworkManager.Singleton.LocalClientId;

            if (!LobbyHandler.Instance) {
                Debug.LogWarning($"Lobby Handler null!");
                return;
            }
            
            if (!LobbyHandler.Instance.PlayersInLobby.ContainsKey(clientId)) {
                Debug.LogWarning($"Player {clientId} to switch teams not found!");
                return;
            }
            
            var oldTeamID = LobbyHandler.Instance.PlayersInLobby[clientId].TeamID;
            
            if (oldTeamID == newTeamID ) {
                print("Already in Team");
                return;
            }
            
            var res = CheckTeamCapacity(false, oldTeamID);
            if (!res) {
                print("Team already empty");
                return;
            }
            
            ResetTeamCapacity();
            
            PlayerSwitchTeam?.Invoke(clientId, newTeamID);
        }

        public int GetTeamID()
        {
            if (_team01Players < _playerRoomCapacity) {
                return 1;
            }
            else if (_team02Players < _playerRoomCapacity) {
                return 2;
            }
            else if (_team03Players < _playerRoomCapacity) {
                return 3;
            }
            
            // All Rooms are full
            return -1;
        }

        private void UpdateTeamCapacity()
        {
            ResetTeamCapacity();
            
            for(int i = 0; i < _currentPlayers.Count; i++) {
                switch (_currentPlayers[(ulong)i].TeamID)
                {
                    case 1:
                        _team01Players++;
                        break;
                    case 2:
                        _team02Players++;
                        break;
                    case 3:
                        _team03Players++;
                        break;
                }
            }
        }

        private void ResetTeamCapacity()
        {
            _team01Players = 0;
            _team02Players = 0;
            _team03Players = 0;
        }
        
        private bool CheckTeamCapacity(bool isJoining, int teamID)
        {
            UpdateTeamCapacity();
            
            // IF Is joining add to team number            
            if (isJoining)
            {
                switch (teamID) {
                    case 1:
                        if (_team01Players < _playerRoomCapacity) {
                            return true;

                        } break;
                    case 2:
                        if (_team02Players < _playerRoomCapacity) {
                            return true;
                        } break;
                    case 3:
                        if (_team03Players < _playerRoomCapacity) {
                            return true;
                        } break;
                    default:
                        if (_team01Players < _playerRoomCapacity) {
                            return true;
                        }
                        else {
                            Debug.LogWarning("Rooms already empty");
                            return false;
                        }
                        break;
                }

                return false;
            }
            
            // Is Leaving Team
            switch (teamID) {
                case 1:
                    if (_team01Players > 0) { 
                        return true;
                    }
                    break;
                case 2:
                    if (_team02Players > 0) {
                        return true;
                    }
                    break;
                case 3:
                    if (_team03Players > 0) {
                        return true;
                    }
                    break;
                default:
                    if (_team01Players > 0) {
                        return true;
                    }
                    else {
                        Debug.LogWarning("Rooms already empty");
                        return false;
                    }
                    break;
            }
            print("Capacity 1: " + _team01Players + " 2: " + _team02Players + " 3: " + _team03Players );
            return false;
        }
    }
}