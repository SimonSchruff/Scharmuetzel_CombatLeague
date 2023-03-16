using System;
using System.Collections.Generic;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

namespace MainProject.Scripts.Manager
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        // Net Vars
        public NetworkVariable<TeamPoints> Net_TeamPoints = new NetworkVariable<TeamPoints>();
        public NetworkVariable<FlagGameState> Net_FlagGameState = new NetworkVariable<FlagGameState>();
        
        [Header("Game Settings")]
        [SerializeField] private float time_before_start = 10.0f;
        public float PointsToWin = 100f;
        [SerializeField] private float PointsPerSecond = 1f;
        [Space(10)]
        [SerializeField] private float RespawnTime = 2f;
        [SerializeField] private float MaxRespawnTime = 10f;
        [SerializeField] private float RespawnGameTimeFactor = 0.10f;

        private bool has_game_started = false;
        private bool is_game_over = false;
        
        public float game_time { get; private set; }
        private float tick_rate;
        private float points_per_tick;

        public event Action<int> OnTeamWon;
        public event Action<float> OnGameStarted;

        private void Awake()
        {
            game_time = 0f - time_before_start; 
        }

        public override void OnNetworkSpawn()
        {
            tick_rate = NetworkManager.NetworkTickSystem.TickRate;
            points_per_tick = PointsPerSecond / tick_rate;

            NetworkManager.Singleton.NetworkTickSystem.Tick += TickGame;
            
            if (!IsServer) { SyncGameTimeServerRpc();}
            //if (IsServer) { SyncGameTimeClientRpc(game_time);}
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkTickSystem != null) {
                NetworkManager.Singleton.NetworkTickSystem.Tick -= TickGame;
            }
        }

        private void TickGame()
        {
            game_time += 1f / tick_rate;
            
            if (!IsServer) { return;}
            
            if(game_time >= 0 && !has_game_started) {
                has_game_started = true;
                StartGameClientRpc();
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void StartGameClientRpc()
        {
            print("Start game CLIENT RPC");
            
            has_game_started = true;
            OnGameStarted?.Invoke(game_time);
        }

        /// <summary>
        /// Count up the points for a team per tick;
        /// </summary>
        public void AddTeamPoints(int teamId)
        {
            if (!IsServer || is_game_over || !has_game_started) { return; }

            var currentPoints = Net_TeamPoints.Value;
            
            if (teamId == 1) {
                currentPoints.Points01 += points_per_tick;
                if (currentPoints.Points01 >= PointsToWin) {
                    Win(1);
                }
            }
            else if (teamId == 2) {
                currentPoints.Points02 += points_per_tick;
                if (currentPoints.Points02 >= PointsToWin) {
                    Win(2);
                }
            }
            else {
                Debug.LogError("Wrong teamId! Cannot give points to any team;");
                return;
            }

            Net_TeamPoints.Value = currentPoints;
        }

        public void OnChangeFlagGameState(int flagIndex, FlagState newState)
        {
            if (!IsServer || is_game_over || !has_game_started) { return;}

            var currentState = Net_FlagGameState.Value;
            
            switch (flagIndex)
            {
                case 1:
                    currentState.FlagState01 = newState;
                    break;
                case 2:
                    currentState.FlagState02 = newState;
                    break;
                case 3:
                    currentState.FlagState03 = newState;
                    break;
                default:
                    Debug.LogWarning("Wrong flag index given! Cannot change game state!");
                    return;
                    break;
            }

            Net_FlagGameState.Value = currentState;
        }


        /// <summary>
        /// This method returns the local estimate of the current game time;
        /// Only server has correct game time;
        /// </summary>
        /// <returns>game time in seconds</returns>
        public float GetLocalCurrentGameTime()
        {
            return game_time;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SyncGameTimeServerRpc()
        {
            if (!IsServer) { return; }
            SyncGameTimeClientRpc(game_time);
        }
        
        [ClientRpc]
        private void SyncGameTimeClientRpc(float serverTime)
        {
            if (IsServer) { return; }
            game_time = serverTime;
        }
        
        public float GetRespawnTime()
        {
            float time = RespawnTime + (game_time * RespawnGameTimeFactor);
            return Mathf.Clamp(time, RespawnTime, MaxRespawnTime);
        }
        
        private void Win(int teamId)
        {
            if (!IsServer || is_game_over) { return;}
            
            print($"Team {teamId} has won this round!");
            TeamWinClientRpc(teamId);
            is_game_over = true;
        }

        [ClientRpc]
        private void TeamWinClientRpc(int teamId)
        {
            OnTeamWon?.Invoke(teamId);
        }
    }
}
