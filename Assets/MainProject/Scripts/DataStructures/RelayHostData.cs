using System;

namespace MainProject.Scripts.DataStructures
{
    public struct RelayHostData
    {
        public string LobbyName;
        public int MaxPlayers;
        public string MapName;
        public bool IsPublic;
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }
}
