using System.Collections;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Lobby
{
    public class LobbyDisplayPlayer : MonoBehaviour
    {

        [Header("Refs")]
        [SerializeField] private GameObject _playerObject;
        [SerializeField] private ParticleSystem _spawnFXTeam01;
        [SerializeField] private ParticleSystem _spawnFXTeam02;
        [SerializeField] private TextMeshProUGUI _playerNameText;

        private Animator _animator;
        private const string _idleTrigger01 = "1H_B";
        private const string _idleTrigger02 = "GS";
        private const string _idleTrigger03 = "2H";
        
        private int _idleTriggerHash01;
        private int _idleTriggerHash02;
        private int _idleTriggerHash03;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _idleTriggerHash01 = Animator.StringToHash(_idleTrigger01);
            _idleTriggerHash02 = Animator.StringToHash(_idleTrigger02);
            _idleTriggerHash03 = Animator.StringToHash(_idleTrigger03);
        }

        public void SetPlayerObjectActiveAfterTime(int teamID, float sec = 0f, string playerName = "playerName")
        {
            StartCoroutine(SpawnPlayerObject(teamID, sec,  playerName));
        }

        private IEnumerator SpawnPlayerObject(int teamID, float time, string playerName)
        {
            yield return new WaitForSeconds(time);

            switch (teamID) {
                case 1:
                    _spawnFXTeam01.Play();
                    break;
                case 2:
                    _spawnFXTeam02.Play();
                    break;
            }
            
            yield return new WaitForSeconds(0.2f);
            
            PlayRandomIdleAnim();

            _playerObject.SetActive(true);
            _playerNameText.gameObject.SetActive(true);
            _playerNameText.text = playerName;
        
        }

        public void DisablePlayerObject()
        {
            _playerObject.SetActive(false);
            _playerNameText.gameObject.SetActive(false);
        }

        private void PlayRandomIdleAnim()
        {
            if (_animator == null) { return; }
            
            var random = UnityEngine.Random.Range(0, 4);
            switch (random)
            {
                case 0:
                    // default anim
                    break;
                case 1:
                    _animator.SetTrigger(_idleTriggerHash01);
                    break;
                case 2:
                    _animator.SetTrigger(_idleTriggerHash02);
                    break;
                case 3:
                    _animator.SetTrigger(_idleTriggerHash03);
                    break;
                

            }
        }
    }
}
