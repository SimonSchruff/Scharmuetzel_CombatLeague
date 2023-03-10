using Unity.Netcode;

namespace MainProject.Scripts.DataStructures
{
    public struct PlayerLobbyData : INetworkSerializable
    {
        public ulong ID;
        public string PlayerName;
        public bool IsReady;
        public int TeamID;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref ID);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref TeamID);
        }
    }
}