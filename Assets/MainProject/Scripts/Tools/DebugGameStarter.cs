using System;
using System.Collections;
using System.Runtime.InteropServices;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Tools.Services;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Task = System.Threading.Tasks;

namespace MainProject.Scripts.Tools
{
    public class DebugGameStarter : MonoBehaviour
    {
        public static event Action<int> OnHostOrClientStarted;  

        [Header("UI Elements")]
        public Button HostButton; 
        public Button QuitButton;
        public Button ReloadButton; 
        public TMP_InputField InputField;
        public GameObject ControlsDisplay;
        
        [Header("Prefabs")]
        public GameObject SpectatorPrefab; 
        
        void Start()
        {
            // Start Host
            HostButton?.onClick.AddListener(async () =>
            {
                using (new Load("Starting lobby..."))
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                
                    await Authentication.Login();

                    var relayData = new RelayHostData {
                        LobbyName = "test",
                        IsPublic = true,
                    };
                
                    await MatchmakingService.CreateLobbyWithAllocation(relayData);
                
                    await System.Threading.Tasks.Task.Delay(1000);

                    if (NetworkManager.Singleton.StartHost()) {
                        this.gameObject.SetActive(false);
                        ControlsDisplay.SetActive(false);
                        Debug.Log("Host started successfully...");
                        OnHostOrClientStarted?.Invoke(1);
                        await System.Threading.Tasks.Task.Delay(2000);
                    }
                    else {
                        Debug.Log("Unable to start host...");
                        CanvasUtils.Instance.ShowError("Something went wrong! Try reloading the scene!");
                    }
                }
            });
            
            QuitButton.onClick.AddListener(() => {
                Debug.LogWarning("Application was quit!");
                Application.Quit();
            });
            
            ReloadButton.onClick.AddListener(() => {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
        }

        public async void StartClient(int teamId)
        {
            using (new Load("Joining lobby..."))
            {
                await System.Threading.Tasks.Task.Delay(1000);

                await Authentication.Login();

                string code = InputField.text;

                if (String.IsNullOrEmpty(code) || code.Length != 6) {
                    Debug.Log("JoinCode Empty or false!");
                    CanvasUtils.Instance.ShowError("The join code you entered is not valid!");
                    return;
                }
            
                await MatchmakingService.JoinLobbyWithJoinCode(code);
            
                await System.Threading.Tasks.Task.Delay(1000);

                if (NetworkManager.Singleton.StartClient()) {
                    this.gameObject.SetActive(false);
                    ControlsDisplay.SetActive(false);
                    
                    Debug.Log("Client joined successfully...");
                    OnHostOrClientStarted?.Invoke(teamId);
                    await System.Threading.Tasks.Task.Delay(2000);
                }
                else {
                    CanvasUtils.Instance.ShowError("Something went wrong! Try reloading the scene!");
                    Debug.Log("Unable to join as client...");
                }
            }
            
        }
        
        public async void StartClientSpectator()
        {
            using (new Load("Joining lobby..."))
            {
                await System.Threading.Tasks.Task.Delay(1000);

                await Authentication.Login();

                string code = InputField.text;

                if (String.IsNullOrEmpty(code) || code.Length != 6) {
                    Debug.Log("JoinCode Empty or false!");
                    CanvasUtils.Instance.ShowError("The join code you entered is not valid!");
                    return;
                }
            
                await MatchmakingService.JoinLobbyWithJoinCode(code);
            
                await System.Threading.Tasks.Task.Delay(1000);

                if (NetworkManager.Singleton.StartClient()) {
                    this.gameObject.SetActive(false);
                    ControlsDisplay.SetActive(false);

                    Debug.Log("Client joined successfully...");
                    
                    var obj = Instantiate(SpectatorPrefab, Vector3.zero + Vector3.up * 10f, Quaternion.identity);
                }
                else {
                    CanvasUtils.Instance.ShowError("Unable to join as client...");
                }
            }
            
        }
    }
}
