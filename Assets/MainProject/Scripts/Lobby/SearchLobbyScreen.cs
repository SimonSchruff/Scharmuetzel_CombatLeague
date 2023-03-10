using System;
using System.Collections.Generic;
using System.Linq;
using MainProject.Scripts.Tools;
using MainProject.Scripts.Tools.Services;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class SearchLobbyScreen : MonoBehaviour {
        [SerializeField] private LobbyRoomPanel _lobbyPanelPrefab;
        [SerializeField] private Transform _lobbyParent;
        [SerializeField] private GameObject _noLobbiesText;
        [SerializeField] private TMP_InputField _lobbyCodeInputField;
        [SerializeField] private float _lobbyRefreshRate = 2;

        private readonly List<LobbyRoomPanel> _currentLobbySpawns = new();
        private float _nextRefreshTime;
        
        public static event Action<string> OnLobbyJoinWithCode;


        private void Update() {
            if (Time.time >= _nextRefreshTime) FetchLobbies();
        }

        private void OnEnable() {
            foreach (Transform child in _lobbyParent) Destroy(child.gameObject);
            _currentLobbySpawns.Clear();
        }

        private async void FetchLobbies() {
            try {
                _nextRefreshTime = Time.time + _lobbyRefreshRate;

                // Grab all current lobbies
                var allLobbies = await MatchmakingService.GatherLobbies();

                // Destroy all the current lobby panels which don't exist anymore.
                // Exclude our own homes as it'll show for a brief moment after closing the room
                var lobbyIds = allLobbies.Where(l => l.HostId != Authentication.PlayerId).Select(l => l.Id);
                var notActive = _currentLobbySpawns.Where(l => !lobbyIds.Contains(l.Lobby.Id)).ToList();

                foreach (var panel in notActive) {
                    _currentLobbySpawns.Remove(panel);
                    Destroy(panel.gameObject);
                }

                // Update or spawn the remaining active lobbies
                foreach (var lobby in allLobbies) {
                    var current = _currentLobbySpawns.FirstOrDefault(p => p.Lobby.Id == lobby.Id);
                    if (current != null) {
                        current.UpdateDetails(lobby);
                    }
                    else {
                        var panel = Instantiate(_lobbyPanelPrefab, _lobbyParent);
                        panel.Init(lobby);
                        _currentLobbySpawns.Add(panel);
                    }
                }

                _noLobbiesText.SetActive(_currentLobbySpawns.Count < 1);
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }

        public void JoinLobbyWithCode()
        {
            var lobbyCode = _lobbyCodeInputField.text;
            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6) {
                CanvasUtils.Instance.ShowError("Lobby code is not valid!");
                return;
            }
            
            OnLobbyJoinWithCode?.Invoke(lobbyCode);
        }
    }
}