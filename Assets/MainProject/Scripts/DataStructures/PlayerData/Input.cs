namespace MainProject.Scripts.DataStructures.PlayerData
{
    public enum KeyState : byte {
        off = 1 << 0, 
        press = 1 << 1,
        held = 1 << 2,
        release = 1 << 3,
    }
}