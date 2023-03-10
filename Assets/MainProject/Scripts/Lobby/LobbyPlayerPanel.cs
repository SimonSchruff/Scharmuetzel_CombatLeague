using MainProject.Scripts.DataStructures;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class LobbyPlayerPanel : MonoBehaviour {
        [SerializeField] private TMP_Text _nameText, _statusText;

        [Header("Settings")]
        [SerializeField] private Color _readyColor;
        [SerializeField] private Color  _waitingColor;

        
        public ulong PlayerId { get; private set; }
        public string PlayerName;
        public int TeamId;
        public bool IsReady;

        public void Init(ulong playerId, string playerName, int teamId, bool isReady) {
            PlayerId = playerId;
            TeamId = teamId;
            PlayerName = playerName;
            IsReady = isReady;
            SetNameText();
        }

        public void SetReady()
        {
            IsReady = true;
            SetNameText();
        }

        public void SetPending()
        {
            IsReady = false;
            SetNameText();
        }

        public void SetNameText()
        {
            if (IsReady) {
                _nameText.color = _readyColor;
                _nameText.text = $"#{PlayerId} - {PlayerName} - Ready...";
            }
            else {
                _nameText.color = _waitingColor;
                _nameText.text = $"#{PlayerId} - {PlayerName} - Waiting...";
            }
            
        }

        
    }
}