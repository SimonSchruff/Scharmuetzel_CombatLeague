using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace MainProject.Scripts.Tools.Services
{
    public static class Authentication{
        public static string PlayerId { get; private set; }

        public static async Task Login() {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) {
                var options = new InitializationOptions();


#if UNITY_EDITOR
                // It's used to differentiate the clients, otherwise lobby will count them as the same
                if (ClonesManager.IsClone()) {
                    options.SetProfile(ClonesManager.GetArgument());
                }
                else {
                    options.SetProfile("Primary");
                }
#endif

#if DEVELOPMENT_BUILD
                var time = DateTime.Now;
                options.SetProfile($"Client_{time}_Profile");
#endif
                
                // TODO: For local build testing we need to also set profile
                // https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_locally/index.html
                    
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn) 
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerId = AuthenticationService.Instance.PlayerId;
                Debug.Log($"User authenticated with with playerID {PlayerId}");
            }
        }

        
        /// <summary>
        /// Return whether or not the user is signed in with authentication service
        /// </summary>
        public static bool IsLoggedIn()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                return false;
            }
            
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    
    
}