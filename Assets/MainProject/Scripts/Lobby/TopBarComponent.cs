using MainProject.Scripts.DataStructures;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class TopBarComponent : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerNameText;

        private void Start()
        {
            _playerNameText.text = Constants.PlayerName;
        }
    }
}
