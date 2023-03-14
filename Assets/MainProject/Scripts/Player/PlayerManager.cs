using System;
using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Player.PlayerUI;
using MainProject.Scripts.Tools;
using MainProject.Scripts.Tools.Services;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class PlayerManager : NetworkSingleton<PlayerManager>
    {
        [Header("Settings")] public bool IsDebugModeEnabled = true;

        [Header("Player Prefab")] 
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _spectatorPrefab;
        [Header("Player Materials")] 
        [SerializeField] public Material PlayerTeam01Material;
        [SerializeField] public Material PlayerTeam02Material;
        
        [Header("Spawn Positions")] 
        public Transform[] Team01_SpawnPositions = new Transform[3];
        public Transform[] Team02_SpawnPositions = new Transform[3];

        private Dictionary<int, Player.Character> Team01_ActivePlayers = new Dictionary<int, Player.Character>();
        private Dictionary<int, Player.Character> Team02_ActivePlayers = new Dictionary<int, Player.Character>();

        private void Awake()
        {
            if (IsDebugModeEnabled)
            {
                DebugGameStarter.OnHostOrClientStarted += OnSpawnPlayer;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsDebugModeEnabled) {
                return;
            }
            
            var players = NetworkSaveManager.Instance.LobbyPlayerData;
            var player = players[NetworkManager.Singleton.LocalClientId];
            
            // Spawn as spectator local only
            if (player.TeamID == 3) {
                print("IsSpectator");
                var obj = Instantiate(_spectatorPrefab, Vector3.zero + Vector3.up * 10f, Quaternion.identity);
            }
            
            // Spawn player objects as server
            if ( IsServer) {
                for (ulong i = 0; i < (ulong)players.Count; i++)
                {
                    if (players[i].TeamID != 3) {
                        SpawnPlayer(players[i].ID , players[i].TeamID, players[i].PlayerName);
                    }
                }
            }
        }

        private void SpawnPlayer(ulong clientId, int teamID, string playerName)
        {
            if (!IsServer) {
                return;
            }
            
            // Assign spawn location
            Vector3 spawnPos;
            if (teamID == 1) {
                var locationIndex = Team01_ActivePlayers.Count;
                spawnPos = Team01_SpawnPositions[locationIndex].position;
            }
            else {
                var locationIndex = Team02_ActivePlayers.Count;
                spawnPos = Team02_SpawnPositions[locationIndex].position;
            }

            // spawn player obj
            var playerGO = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            var player = playerGO.GetComponent<Character>();
            player.NetworkObject.SpawnAsPlayerObject(clientId);

            // Set up relevant net vars of character
            // TODO: Set up name properly from lobby
            player.Net_PlayerName.Value = playerName;
            player.Net_TeamID.Value = teamID;

            if (teamID == 1) {
                Team01_ActivePlayers.Add((int) clientId, player);
            }
            else {
                Team02_ActivePlayers.Add((int) clientId, player);
            }
            
            print($"New Player ˚{player.Net_PlayerName.Value}˚ has entered game;");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (IsDebugModeEnabled)
            {
                DebugGameStarter.OnHostOrClientStarted -= OnSpawnPlayer;
            }

            AwaitLeaveLobby();

            // TODO: Remove player from game with server rpc
        }

        private async void AwaitLeaveLobby()
        {
            await MatchmakingService.LeaveLobby();

            if (NetworkManager.Singleton != null) {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public void SpawnPlayerForLocalClient(int teamID)
        {
            StartCoroutine(SpawnPlayerAfterSec(teamID));
        }

        private void OnSpawnPlayer(int teamId)
        {
            StartCoroutine(SpawnPlayerAfterSec(teamId));
        }

        private IEnumerator SpawnPlayerAfterSec(int teamId)
        {
            yield return new WaitForSeconds(1f);

            SpawnPlayerServerRpc(teamId);
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnPlayerServerRpc(int teamID, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;

            // Assign spawn location
            Vector3 spawnPos;
            if (teamID == 1) {
                var locationIndex = Team01_ActivePlayers.Count;
                spawnPos = Team01_SpawnPositions[locationIndex].position;
            }
            else {
                var locationIndex = Team02_ActivePlayers.Count;
                spawnPos = Team02_SpawnPositions[locationIndex].position;
            }

            // spawn player obj
            var playerGO = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            var player = playerGO.GetComponent<Character>();
            player.NetworkObject.SpawnAsPlayerObject(clientId);

            // Set up relevant net vars of character
            // TODO: Set up name properly from lobby
            player.Net_PlayerName.Value = $"Player {player.OwnerClientId}";
            player.Net_TeamID.Value = teamID;

            if (teamID == 1) {
                Team01_ActivePlayers.Add((int) clientId, player);
            }
            else {
                Team02_ActivePlayers.Add((int) clientId, player);
            }
            
            print($"New Player ˚{player.Net_PlayerName.Value}˚ has entered game;");
        }
        
        public void SetPlayerToSpawnPosition(int playerId)
        {
            if (!IsServer) { return;}
            
            if (Team01_ActivePlayers.ContainsKey(playerId))
            {
                var character = Team01_ActivePlayers[playerId];
                var teamId = character.Net_TeamID.Value;

                MovePlayerToSpawnPos(teamId, character);
            }

            if (Team02_ActivePlayers.ContainsKey(playerId))
            {
                var character = Team02_ActivePlayers[playerId];
                var teamId = character.Net_TeamID.Value;

                MovePlayerToSpawnPos(teamId, character);
            }
        }
        
        private void MovePlayerToSpawnPos(int teamId, Character player)
        {
            if (!IsServer) { return;}
            
            Vector3 spawnPos;
            if (teamId == 1) {
                var locationIndex = Team01_ActivePlayers.Count - 1;
                spawnPos = Team01_SpawnPositions[locationIndex].position;
            }
            else {
                var locationIndex = Team02_ActivePlayers.Count - 1;
                spawnPos = Team02_SpawnPositions[locationIndex].position;
            }

            player.transform.position = spawnPos;
            player.LinkedFXHandler.PlaySpawnFX(teamId);

        }
    }
}