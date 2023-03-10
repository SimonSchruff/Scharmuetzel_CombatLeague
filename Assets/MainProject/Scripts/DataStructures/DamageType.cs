using System;
using MainProject.Scripts.DataStructures.PlayerData;
using UnityEngine;

namespace MainProject.Scripts.DataStructures
{
    [Serializable]
    public class DamageType
    {
        // RAW DAMAGE
        [Tooltip("The amount of damaged applied")]
        public float DamageCaused;
        
        // CONDITION 
        [Tooltip("whether or not this damage, when applied, should force the character into a specified condition")] 
        public bool ForceCharacterCondition = false;
        [Tooltip("The condition this damage will cause;")]
        public CharacterStates.CharacterConditions Condition;
        [Tooltip("The time the condition of this damage will take effect")]
        public float ConditionDuration;
        
        // MOVEMENT
        [Tooltip("whether or not to apply a movement multiplier to the damaged character")] 
        public bool ApplyMovementMultiplier = false;
        [Tooltip("the movement multiplier to apply when ApplyMovementMultiplier is true")]
        public float MovementMultiplier = 0.5f;
        [Tooltip("the duration of the movement multiplier, if ApplyMovementMultiplier is true")]
        public float MovementMultiplierDuration = 2f;
    }
}