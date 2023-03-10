using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MainProject.Scripts.DataStructures;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Netcode;
using MainProject.Scripts.Tools;

using Object = UnityEngine.Object;

namespace MainProject.Scripts.Tools.Services
{
    public static class MatchmakingService {
        private const int HeartbeatInterval = 15;
        private const int LobbyRefreshRate = 2; // Rate limits at 2
        
        private const int MAX_PLAYERS = 9;
        private const string _environment = "production";

        private static UnityTransport _transport;
        

        public static Unity.Services.Lobbies.Models.Lobby CurrentLobby  => _currentLobby;
        
        private static Unity.Services.Lobbies.Models.Lobby _currentLobby;
        
        private static CancellationTokenSource _heartbeatSource, _updateLobbySource;

        private static UnityTransport Transport {
            get => _transport != null ? _transport : _transport = Object.FindObjectOfType<UnityTransport>();
            set => _transport = value;
        }

        public static event Action<Unity.Services.Lobbies.Models.Lobby> CurrentLobbyRefreshed;
        public static event Action<string> OnLobbyCreated;


        public static void ResetStatics() {
            if (Transport != null) {
                Transport.Shutdown();
                Transport = null;
            }

            _currentLobby = null;
        }

        // Obviously you'd want to add customization to the query, but this
        // will suffice for this simple demo
        public static async Task<List<Unity.Services.Lobbies.Models.Lobby>> GatherLobbies() {
            var options = new QueryLobbiesOptions {
                Count = 15,

                Filters = new List<QueryFilter> {
                    new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                }
            };

            var allLobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            return allLobbies.Results;
        }

        
    public static async Task CreateLobbyWithAllocation(RelayHostData data)
    {
        // Create a relay allocation and generate a join code to share with the lobby
        var a = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

        RelayHostData relayHostData = new RelayHostData
        {
            Key = a.Key,
            JoinCode = joinCode,
            MapName = data.MapName,
            MaxPlayers = MAX_PLAYERS,
            Port = (ushort) a.RelayServer.Port,
            AllocationID = a.AllocationId,
            AllocationIDBytes = a.AllocationIdBytes,
            IPv4Address = a.RelayServer.IpV4,
            ConnectionData = a.ConnectionData
        };

        var lobbyOptions = new CreateLobbyOptions
        {
            IsPrivate = !data.IsPublic,
            Data = new Dictionary<string, DataObject>
            {
                {Constants.RelayJoinKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode)},
                {Constants.CurrentMapName, new DataObject(DataObject.VisibilityOptions.Public, data.MapName)},
                //{Constants.DifficultyKey, new DataObject(DataObject.VisibilityOptions.Public, data.Type.ToString(), DataObject.IndexOptions.N2)}
            }
        };
        
        
        _currentLobby = await Lobbies.Instance.CreateLobbyAsync(data.LobbyName, MAX_PLAYERS, lobbyOptions);

        Transport.SetHostRelayData(relayHostData.IPv4Address, relayHostData.Port, relayHostData.AllocationIDBytes, relayHostData.Key, relayHostData.ConnectionData);

        Heartbeat();
        PeriodicallyRefreshLobby();
        
        Debug.Log($"Lobby Created: " + " Lobby Code copied to clipboard;" +
                  $"\n Lobby Code: {_currentLobby.LobbyCode} ;" +
                  $"\n Relay Code: {joinCode} ; " +
                  $"\n IP: {relayHostData.IPv4Address} ;");

        
        // Copy code to clipboard
        GUIUtility.systemCopyBuffer = _currentLobby.LobbyCode;
        OnLobbyCreated?.Invoke(_currentLobby.LobbyCode);
    }
    

        public static async Task LockLobby() {
            try {
                await Lobbies.Instance.UpdateLobbyAsync(_currentLobby.Id, new UpdateLobbyOptions { IsLocked = true });
            }
            catch (Exception e) {
                Debug.Log($"Failed closing lobby: {e}");
            }
        }

        private static async void Heartbeat() {
            
            _heartbeatSource = new CancellationTokenSource();
            while (!_heartbeatSource.IsCancellationRequested && _currentLobby != null) {
                await Lobbies.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                await Task.Delay(HeartbeatInterval * 1000);
            }
        }

        
        public static async Task JoinLobbyWithAllocation(string lobbyId) {
            _currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            
            var a = await RelayService.Instance.JoinAllocationAsync(_currentLobby.Data[Constants.RelayJoinKey].Value);

            Transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
            Debug.Log($"Client Joined Lobby with IP: {a.RelayServer.IpV4}");


            PeriodicallyRefreshLobby();
        }
        
        public static async Task JoinLobbyWithJoinCode(string joinCode)
        {
            try {
                var options = new JoinLobbyByCodeOptions();
                _currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(joinCode, options);

                var a = await RelayService.Instance.JoinAllocationAsync(_currentLobby.Data[Constants.RelayJoinKey].Value);
                Transport.SetClientRelayData(a.RelayServer.IpV4, (ushort) a.RelayServer.Port, a.AllocationIdBytes, a.Key,
                    a.ConnectionData, a.HostConnectionData);
            }
            catch (Exception err) {
                Debug.LogWarning(err);
            }
            
            PeriodicallyRefreshLobby();
        }
        

        private static async void PeriodicallyRefreshLobby() {
            _updateLobbySource = new CancellationTokenSource();
            await Task.Delay(LobbyRefreshRate * 1000);
            while (!_updateLobbySource.IsCancellationRequested && _currentLobby != null) {
                _currentLobby = await Lobbies.Instance.GetLobbyAsync(_currentLobby.Id);
                CurrentLobbyRefreshed?.Invoke(_currentLobby);
                await Task.Delay(LobbyRefreshRate * 1000);
            }
        }

        public static async Task LeaveLobby() {
            _heartbeatSource?.Cancel();
            _updateLobbySource?.Cancel();

            if (_currentLobby != null)
                try {
                    if (_currentLobby.HostId == Authentication.PlayerId) await Lobbies.Instance.DeleteLobbyAsync(_currentLobby.Id);
                    else await Lobbies.Instance.RemovePlayerAsync(_currentLobby.Id, Authentication.PlayerId);
                    _currentLobby = null;
                }
                catch (Exception e) {
                    Debug.Log(e);
                }
        }
    }
}