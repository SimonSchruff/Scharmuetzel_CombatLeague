using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Specialized;

namespace MainProject.Scripts.DataStructures.PlayerData
{
    /// <summary>
    /// Message that is sent between client;
    /// Contains Tick number of last input, and redundant inputs with according deltaTime;
    /// </summary>
    public struct InputMessage : INetworkSerializable
    {
        // Limit ~508 bytes for unreliable rpc's
        public uint client_tick_number;
        public float delta_time;
        public PlayerInputs[] player_inputs;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref client_tick_number);
            serializer.SerializeValue(ref delta_time);

            serializer.SerializeValue(ref player_inputs);

            // Define Length of array
            int length = 0;
            if (!serializer.IsReader) {
                length = player_inputs.Length;
            }
            serializer.SerializeValue(ref length);
            
            // Read Array
            if (serializer.IsReader) {
                player_inputs = new PlayerInputs[length];
            }
            
            // Serialize Input Array Elements
            for (int n = 0; n < length; n++) {
                serializer.SerializeValue(ref player_inputs[n]);
            }
            
        }
    }

    /*
    public struct InputBits : INetworkSerializable
    {
        public BitVector32 bitVector;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsWriter)
            {
                serializer.GetFastBufferWriter().WriteValueSafe(bitVector.Data);
            }
 
            if(serializer.IsReader)
            {
                serializer.GetFastBufferReader().ReadValueSafe<int>(out int bitVectorData);
 
                bitVector = new BitVector32(bitVectorData);
 
                // Debug.Log("BitVector Struct: " + bitVector.ToString());
            }
        }
    }   
    */
    
    
    public struct PlayerInputs : INetworkSerializable
    {
        // 16 bytes  in total
        // Joysticks (8 bytes)
        public Vector2 left_stick_input; // 8 bytes (4 per float)
        
        // Button enums (1 byte each) (8 total)
        public KeyState basic_attack_input;

        public KeyState dash_input;
        public KeyState heal_input;
        
        public KeyState cast_01_input;
        public KeyState cast_02_input;
        public KeyState cast_03_input;
        public KeyState cast_04_input;


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref left_stick_input);
            
            serializer.SerializeValue(ref basic_attack_input);
            
            serializer.SerializeValue(ref dash_input);
            serializer.SerializeValue(ref heal_input);
            
            serializer.SerializeValue(ref cast_01_input);
            serializer.SerializeValue(ref cast_02_input);
            serializer.SerializeValue(ref cast_03_input);
            serializer.SerializeValue(ref cast_04_input);
        }
    }
}