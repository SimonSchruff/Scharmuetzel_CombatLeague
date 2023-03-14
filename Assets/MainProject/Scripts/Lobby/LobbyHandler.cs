using System;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Tools;
using MainProject.Scripts.Tools.Services;

namespace MainProject.Scripts.Lobby
{
    /// <summary>
    ///     Lobby orchestrator. I put as much UI logic within the three sub screens,
    ///     but the transport and RPC logic remains here. It's possible we could pull
    /// </summary>
    public class LobbyHandler : NetworkSingleton<LobbyHandler>
    {

        [SerializeField] private MainMenuScreen _mainMenuScreen;
        [SerializeField] private SearchLobbyScreen _searchLobbyScreen;
        [SerializeField] private CreateLobbyScreen _createScreen;
        [SerializeField] private RoomScreen _roomScreen;
        
        [Header("Map Names")]
        [SerializeField] private string BigGameSceneToLoad = "Map_Big";
        [SerializeField] private string SmallGameSceneToLoad = "Map_Small";
        
        private void Start() {
            
            _mainMenuScreen.gameObject.SetActive(true);
            _searchLobbyScreen.gameObject.SetActive(false);
            _createScreen.gameObject.SetActive(false);
            _roomScreen.gameObject.SetActive(false);

            CreateLobbyScreen.LobbyCreated += CreateLobby;
            LobbyRoomPanel.LobbySelected += OnLobbySelected;
            RoomScreen.LobbyLeft += OnLobbyLeft;
            RoomScreen.StartPressed += OnGameStart;
            RoomScreen.PlayerSwitchTeam += OnClientSwitchedTeam;
            SearchLobbyScreen.OnLobbyJoinWithCode += OnLobbyJoinWithCode;
        
            NetworkObject.DestroyWithScene = true;
            
        }

        #region Main Lobby

        private async void OnLobbySelected(Unity.Services.Lobbies.Models.Lobby lobby) {
            using (new Load("Joining Lobby...")) {
                try
                {
                    await MatchmakingService.JoinLobbyWithAllocation(lobby.Id);

                    _searchLobbyScreen.gameObject.SetActive(false);
                    _roomScreen.gameObject.SetActive(true);

                    NetworkManager.Singleton.StartClient();
                    
                    if (MenuCameraController.Instance != null) {
                        MenuCameraController.Instance.SwitchToLobbyCam();
                    }
                    
                    UpdateInterface();
                }
                catch (Exception e) {
                    Debug.LogError(e);
                    CanvasUtils.Instance.ShowError("Failed joining lobby");
                }
            }
        }

        private async void OnLobbyJoinWithCode(string lobbyCode)
        {
            if (String.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6) {
                Debug.Log("JoinCode Empty or false!");
                return;
            }

            using (new Load("Joining Lobby..."))
            {
                try {
                    await MatchmakingService.JoinLobbyWithJoinCode(lobbyCode);
                    
                    NetworkManager.Singleton.StartClient();

                    _searchLobbyScreen.gameObject.SetActive(false);
                    _roomScreen.gameObject.SetActive(true);
                    
                    if (MenuCameraController.Instance != null) {
                        MenuCameraController.Instance.SwitchToLobbyCam();
                    }
                    
                    UpdateInterface();

                }
                catch (Exception e) {
                    Debug.LogError(e);
                    CanvasUtils.Instance.ShowError("Failed joining lobby...");
                }
            }
        }

 

        #endregion

        #region Create

        private RelayHostData _relayHostData; 

        private async void CreateLobby(RelayHostData data) {
            using (new Load("Creating Lobby...")) {
                try
                {
                    await MatchmakingService.CreateLobbyWithAllocation(data);

                    _searchLobbyScreen.gameObject.SetActive(false);
                    _createScreen.gameObject.SetActive(false);
                    _roomScreen.gameObject.SetActive(true);
                    
                    // Starting the host immediately will keep the relay server alive
                    NetworkManager.Singleton.StartHost();
                    _relayHostData = data;

                    if (MenuCameraController.Instance != null) {
                        MenuCameraController.Instance.SwitchToLobbyCam();
                    }
                }
                catch (Exception e) {
                    Debug.LogError(e);
                    CanvasUtils.Instance.ShowError("Failed creating lobby");
                }
            }
        }

        #endregion

        #region Room
        public Dictionary<ulong, PlayerLobbyData> PlayersInLobby = new();
        public static event Action<Dictionary<ulong, PlayerLobbyData>> LobbyPlayersUpdated;
        private float _nextLobbyUpdate;

        public override void OnNetworkSpawn()
        {
            var id = NetworkManager.Singleton.LocalClientId;
            var playerName = Constants.PlayerName;
            
            if (IsServer) {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                
                PlayerLobbyData data = new PlayerLobbyData {
                    ID = id,
                    PlayerName = playerName,
                    IsReady = false,
                    TeamID = _roomScreen.GetTeamID(),
                };

                if (data.TeamID == -1) {
                    Debug.LogWarning("Lobby is full!");
                    return;
                }
                
                PlayersInLobby.Add(id, data);
                PropagateToClients();
                UpdateInterface();
            }

            if (!IsServer) {
                SetNameServerRpc(id, playerName);
            }

            // Client uses this in case host destroys the lobby
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            
            // Voice Chat
        }

        private void OnClientSwitchedTeam(ulong playerId, int newTeamID)
        {
            if (IsServer)
            {
                if (!PlayersInLobby.ContainsKey(playerId)) {
                    print("Lobby does not contain player " + playerId);
                    return;
                }
            
                var p = PlayersInLobby[playerId];
                p.TeamID = newTeamID;
                PlayersInLobby[playerId] = p;
            
                print($"Server switch team; ID: {playerId} , Team ID: {p.TeamID}");

                PropagateToClients();
                UpdateInterface();
            }
            else
            {
                SwitchTeamServerRpc(playerId, newTeamID);
            }
                
           
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SwitchTeamServerRpc(ulong playerId, int newTeamID)
        {
            if (!IsServer) {
                return;
            }

            if (!PlayersInLobby.ContainsKey(playerId)) {
                print("Lobby does not contain player " + playerId);
                return;
            }
            
            var p = PlayersInLobby[playerId];
            p.TeamID = newTeamID;
            PlayersInLobby[playerId] = p;
            
            print($"Server Rpc switch team; ID: {playerId} , Team ID: {p.TeamID}");

            
            PropagateToClients();
            UpdateInterface();
        }

        private void OnClientConnectedCallback(ulong playerId) {
            if (!IsServer) {
                return;
            }

            // Add locally
            if (!PlayersInLobby.ContainsKey(playerId))
            {
                PlayerLobbyData data = new PlayerLobbyData {
                    ID = playerId,
                    PlayerName = "Client" + playerId,
                    IsReady = false,
                    TeamID = -1,
                };
                Debug.Log("OnClientConn Add Player" + data.ToString());
                PlayersInLobby.Add(playerId, data);
            }

            PropagateToClients();
            UpdateInterface();
        }

        private void PropagateToClients() {
            foreach (var player in PlayersInLobby)
            {
                UpdatePlayerClientRpc(player.Key, player.Value.PlayerName, player.Value.IsReady, player.Value.TeamID);
            }
        }

        [ClientRpc]
        private void UpdatePlayerClientRpc(ulong clientId, string playerName, bool isReady, int teamID) {
            if (IsServer) return;

            if (!PlayersInLobby.ContainsKey(clientId)) {
                PlayerLobbyData data = new PlayerLobbyData {
                    ID = clientId,
                    PlayerName = playerName,
                    IsReady = isReady,
                    TeamID = teamID,
                };
                PlayersInLobby.Add(clientId, data);
                UpdateInterface();
            }
            else {
                var player = PlayersInLobby[clientId];
                player.PlayerName = playerName;
                player.IsReady = isReady;
                player.TeamID = teamID;
                PlayersInLobby[clientId] = player;
                UpdateInterface();
            }
        }


        public void OnLobbyLeaveClicked()
        {
            OnClientDisconnectCallback(NetworkManager.Singleton.LocalClientId);
        }

        private void OnClientDisconnectCallback(ulong playerId) {
            
            print($"on client disconnect {playerId}");
            
            // Local player leaving
            if(NetworkManager.Singleton.LocalClientId == playerId) {
                OnLobbyLeft();
                return;
            }

            if (IsServer) {
                // Handle locally
                if (PlayersInLobby.ContainsKey(playerId)) PlayersInLobby.Remove(playerId);

                // Propagate all clients
                RemovePlayerClientRpc(playerId);

                UpdateInterface();
            }
            else {
                OnLobbyLeft();
            }
        }

        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId) {
            if (IsServer) return;

            if (PlayersInLobby.ContainsKey(clientId)) PlayersInLobby.Remove(clientId);
            UpdateInterface();
        }

        public void OnReadyClicked() {
            ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ToggleReadyServerRpc(ulong playerId)
        {
            var p = PlayersInLobby[playerId];
            p.IsReady = !p.IsReady;
            PlayersInLobby[playerId] = p;
            
            PropagateToClients();
            UpdateInterface();
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetNameServerRpc(ulong playerId, string playerName)
        {
            if (!PlayersInLobby.ContainsKey(playerId)) {
                PlayerLobbyData data = new PlayerLobbyData {
                    ID = playerId,
                    PlayerName = playerName,
                    IsReady = false,
                    TeamID = _roomScreen.GetTeamID(),
                };
                
                if (data.TeamID == -1) {
                    Debug.LogWarning("Lobby is full!");
                    return;
                }
                PlayersInLobby.Add(playerId, data);
            }
            else {
                var p = PlayersInLobby[playerId];
                p.PlayerName = playerName;
                PlayersInLobby[playerId] = p;
            }
            
            PropagateToClients();
            UpdateInterface();
        }

        private void UpdateInterface() {
            LobbyPlayersUpdated?.Invoke(PlayersInLobby);
        }

        private async void OnLobbyLeft() {
            using (new Load("Leaving Lobby...")) {
                PlayersInLobby.Clear();
                NetworkManager.Singleton.Shutdown();
                await MatchmakingService.LeaveLobby();
                
                _roomScreen.gameObject.SetActive(false);
                _searchLobbyScreen.gameObject.SetActive(false);
                _mainMenuScreen.gameObject.SetActive(true);
                
                if (MenuCameraController.Instance != null) {
                    MenuCameraController.Instance.SwitchToStartCam();
                }
            }
        }
    
        public override void OnDestroy() {
     
            base.OnDestroy();
            CreateLobbyScreen.LobbyCreated -= CreateLobby;
            LobbyRoomPanel.LobbySelected -= OnLobbySelected;
            RoomScreen.LobbyLeft -= OnLobbyLeft;
            RoomScreen.StartPressed -= OnGameStart;
            SearchLobbyScreen.OnLobbyJoinWithCode -= OnLobbyJoinWithCode;

        
            // We only care about this during lobby
            if (NetworkManager.Singleton != null) {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
      
        }
    
        private async void OnGameStart() {
            using (new Load("Starting the game...")) {
                
                try
                {
                    await MatchmakingService.LockLobby();
                    NetworkSaveManager.Instance.SavePlayersInLobby(PlayersInLobby);

                    if (_relayHostData.MapName == "3v3") {
                        NetworkManager.Singleton.SceneManager.LoadScene(BigGameSceneToLoad, LoadSceneMode.Single);
                    }
                
                    if (_relayHostData.MapName == "1v1") {
                        NetworkManager.Singleton.SceneManager.LoadScene(SmallGameSceneToLoad, LoadSceneMode.Single);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to Load Game Scene! Map Name: {_relayHostData.MapName}");
                }
                
            }
            
        }

        #endregion
    }
}
    

