using System.Collections.Generic;
using System.Linq;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Tools;
using Unity.Netcode;

namespace MainProject.Scripts.Manager
{
    public class NetworkSaveManager : NetworkSingleton<NetworkSaveManager>
    {
        public Dictionary<ulong, PlayerLobbyData> LobbyPlayerData { get; private set; }

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(this.gameObject);
        }


        public void SavePlayersInLobby(Dictionary<ulong, PlayerLobbyData> playerDictionary)
        {
            if (IsServer)
            {
                LobbyPlayerData = playerDictionary;
                PropagatePlayerDataClientRpc(playerDictionary.Values.ToArray());
            }
            
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void PropagatePlayerDataClientRpc(PlayerLobbyData[] data)
        {
            LobbyPlayerData = new Dictionary<ulong, PlayerLobbyData>();
            for(int i = 0; i < data.Length; i++) {
                LobbyPlayerData.Add(data[i].ID, data[i]);
            }
        }
    }
}