using MainProject.Scripts.Tools.Services;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class BotBarComponent : MonoBehaviour
    {
        [Header("Text Ref")]
        [SerializeField] private TextMeshProUGUI _onlineText;
        [SerializeField] private TextMeshProUGUI _timeText;

        [Header("Color Ref")]
        [SerializeField] private Color _onlineTextColor;
        [SerializeField] private Color _offlineTextColor;


        private void Start()
        {
            if (!_onlineText) {
                return;
            }
        
            // Set online text
            if (Authentication.IsLoggedIn())
            {
                _onlineText.text = "Online";
                _onlineText.color = _onlineTextColor;

            }
            else
            {
                _onlineText.text = "Offline";
                _onlineText.color = _offlineTextColor;

            }

        }


        void Update()
        {
            if (_timeText) {
                _timeText.text = System.DateTime.Now.ToString("HH:mm");
            }
        }
    }
}
