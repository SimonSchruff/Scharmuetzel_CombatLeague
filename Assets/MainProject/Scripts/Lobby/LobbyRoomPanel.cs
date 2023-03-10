using System;
using MainProject.Scripts.DataStructures;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class LobbyRoomPanel : MonoBehaviour {
        [Header("Refs")]
        [SerializeField] private TMP_Text _lobbyNameText;
        [SerializeField] private TMP_Text _mapNameText;
        [SerializeField] private TMP_Text _playerCountText;

        [SerializeField] private GameObject _bigMapImage;
        [SerializeField] private GameObject _smallMapImage;

        public Unity.Services.Lobbies.Models.Lobby Lobby { get; private set; }

        public static event Action<Unity.Services.Lobbies.Models.Lobby> LobbySelected;

        public void Init(Unity.Services.Lobbies.Models.Lobby lobby) {
            UpdateDetails(lobby);
        }

        public void UpdateDetails(Unity.Services.Lobbies.Models.Lobby lobby) {
            Lobby = lobby;
            _lobbyNameText.text = lobby.Name;
            
            var map = lobby.Data[Constants.CurrentMapName].Value;
            _mapNameText.text = map;
            
            if(map == ("3v3")) {
                _bigMapImage.SetActive(true);
                _smallMapImage.SetActive(false);
            }
            
            if(map == ("1v1")) {
                _bigMapImage.SetActive(false);
                _smallMapImage.SetActive(true);
            }
            
            _playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers} Players";

            int GetValue(string key) {
                return int.Parse(lobby.Data[key].Value);
            }
        }

        public void Clicked() {
            LobbySelected?.Invoke(Lobby);
        }
    }
}