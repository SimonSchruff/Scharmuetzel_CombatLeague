using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using MainProject.Scripts.DataStructures;


namespace MainProject.Scripts.Lobby
{
    public class CreateLobbyScreen : MonoBehaviour {
        [SerializeField] private TMP_InputField _nameInput, _maxPlayersInput;
        [SerializeField] private TMP_Dropdown _typeDropdown;
         [SerializeField] private TMP_Dropdown _mapDropdown;

        private void Start()
        {
            _nameInput.text = $"{Constants.PlayerName}'s Lobby"; 
            
            SetOptions(_typeDropdown, Constants.PrivacyGameType);
            SetOptions(_mapDropdown, Constants.MapName);

            void SetOptions(TMP_Dropdown dropdown, IEnumerable<string> values) {
                dropdown.options = values.Select(type => new TMP_Dropdown.OptionData { text = type }).ToList();
            }
        }

        public static event Action<RelayHostData> LobbyCreated;

        public void OnCreateClicked()
        {
            var relayData = new RelayHostData
            {
                LobbyName = _nameInput.text,
                MaxPlayers = 9,
                IsPublic = _typeDropdown.value == 0 ? true : false,
                MapName = _mapDropdown.value == 0 ? "3v3" : "1v1",
            };

            LobbyCreated?.Invoke(relayData);
        }
    }
}