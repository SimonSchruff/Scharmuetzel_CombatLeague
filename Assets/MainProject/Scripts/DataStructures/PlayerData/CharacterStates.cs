

namespace MainProject.Scripts.DataStructures.PlayerData
{  
    public class CharacterStates
    {
        public enum CharacterConditions : byte
        {
            Normal,
            ForcedMovement, 
            Frozen,
            Paused,
            Dead,
            Stunned,
        };

        public enum CharacterMovementStates : byte
        {
            None,
            Idle,
            Running,
            Dashing,
            Casting,

        };

        [System.Flags]
        public enum PlayerAttackStates
        {
            None = 1 << 0,
            BasicAttack = 1 << 1,
            Cast01 = 1 << 2,
        };
    }
}