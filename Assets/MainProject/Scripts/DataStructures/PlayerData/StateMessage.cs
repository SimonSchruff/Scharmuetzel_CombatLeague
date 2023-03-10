using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.DataStructures.PlayerData
{
    public class StateMessage: INetworkSerializable
    {
        public float delta_time;
        public uint tick_number;
        public CharacterStates.CharacterMovementStates movement_state;
        public CharacterStates.CharacterConditions condition_state;
        public Vector3 position;
        public Quaternion rotation;
        
        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref delta_time);
            serializer.SerializeValue(ref tick_number);
            serializer.SerializeValue(ref movement_state);
            serializer.SerializeValue(ref condition_state);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
        }
    }


    public struct ClientState : INetworkSerializable
    {
        public CharacterStates.CharacterMovementStates movement_state;
        public CharacterStates.CharacterConditions condition_state;
        public Vector3 position;
        public Quaternion rotation;
            
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref movement_state);
            serializer.SerializeValue(ref condition_state);
        } 
    }
    
   
}