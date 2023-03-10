using System;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Tools;
using MainProject.Scripts.Tools.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Core;

namespace MainProject.Scripts.Lobby
{
    public class LogInScreen : MonoBehaviour
    {
        [SerializeField] private string _lobbySceneToLoad = "Lobby";
        [SerializeField] private TMP_InputField _nameInputField;


       
        
        public bool SavePlayerName()
        {
            if (String.IsNullOrEmpty(_nameInputField?.text)) {
                CanvasUtils.Instance.ShowError("Please specify a name!");
                return false;
            }

            Constants.PlayerName = _nameInputField?.text;
            print(Constants.PlayerName);
            return true;
        }

        public async void LogInAnonymously()
        {
            if (!SavePlayerName())
                return;
            
            using (new Load("Logging you in..."))
            {
                await Authentication.Login();
                await UnityServices.InitializeAsync(); 
                SceneManager.LoadSceneAsync(_lobbySceneToLoad);
            }
        }
    }
}