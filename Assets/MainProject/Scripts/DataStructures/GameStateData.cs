using Unity.Netcode;

namespace MainProject.Scripts.DataStructures
{
    
    public enum FlagState : byte {
        // None = 1 << 0,
        Neutral = 1 << 0, 
        Paused = 1 << 1,
        Team01Charging = 1 << 2,
        Team02Charging = 1 << 3,
        Team01Decharging = 1 << 4,
        Team02Decharging = 1 << 5,
        Team01Owned = 1 << 6,
        Team02Owned = 1 << 7,
    }
    
    public struct FlagGameState : INetworkSerializable
    {
        public FlagState FlagState01;
        public FlagState FlagState02;
        public FlagState FlagState03;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref FlagState01);
            serializer.SerializeValue(ref FlagState02);
            serializer.SerializeValue(ref FlagState03);
        }
    }

    public struct TeamPoints : INetworkSerializable
    {
        public float Points01;
        public float Points02;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Points01);
            serializer.SerializeValue(ref Points02);
        }
    }  
    
}