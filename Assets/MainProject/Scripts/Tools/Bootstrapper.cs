using MainProject.Scripts.Tools.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace MainProject.Scripts.Tools
{
    /// <summary>
    ///     This will run once before any other scene script
    /// </summary>
    public static class Bootstrapper {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            MatchmakingService.ResetStatics();
            Addressables.InstantiateAsync("CanvasUtils");
        }
    }
}