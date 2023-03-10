using System.Collections.Generic;

namespace MainProject.Scripts.DataStructures
{
    public class Constants
    {
        public static string PlayerName = "s";
        public static string CurrentMapName = "s";
        public static int TeamID = 0;
        public const string RelayJoinKey = "j";

        
        public static readonly List<string> PrivacyGameType = new() { "Public", "Private" };
        public static readonly List<string> MapName = new() { "3v3", "1v1" };
    }
}