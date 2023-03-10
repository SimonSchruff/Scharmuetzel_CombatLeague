using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace MainProject.Scripts.Player.PlayerUI
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Name")] 
        public TextMeshProUGUI PlayerName;
        
        [Header("Health Sliders")]
        public Slider HealthSlider;
        public Slider HealthBackgroundSlider;
        public Image Frame;
        
        
        [Header("Settings")]
        [Tooltip("Modifies the position of the health bar in world space")]
        [SerializeField] private Vector3 PositionOffset;
        [SerializeField] private float LerpSpeedLocalPlayer = 10f;
        [SerializeField] private float LerpSpeedRemotePlayer = 100f;

        private Character _character;
        private Health _health;
        private Camera _mainCamera;
        
        private float _lerpSpeed;
        private float _maxHealth;

        private bool _isInitalized = false;

        public void InitalizeHealthBar(Character character)
        {
            _character = character;
            _health = _character.LinkedHealth;
            _maxHealth = _health.MaxHealth;

            // Set Lerp Position speed of health bar to avoid jittering on local player;
            _lerpSpeed = _character.IsLocalPlayer ? LerpSpeedLocalPlayer : LerpSpeedRemotePlayer;
            
            _health.Net_CurrentHealth.OnValueChanged += OnHealthChanged;
            _health.OnDeath += OnDeath;
            _health.OnRespawn += OnRespawn;
            
            _mainCamera = Camera.main;

            DisableUIElements();
            StartCoroutine( EnableAfterTime(4f));

            _isInitalized = true;
        }

        private void OnDestroy()
        {
            if (!_isInitalized) { return; }
            _health.Net_CurrentHealth.OnValueChanged -= OnHealthChanged;
            _health.OnDeath -= OnDeath;
            _health.OnRespawn -= OnRespawn;
        }

        private IEnumerator EnableAfterTime(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            EnableUIElements();
        }

        private void EnableUIElements()
        {
            HealthSlider.gameObject.SetActive(true);
            HealthBackgroundSlider.gameObject.SetActive(true);
            PlayerName.enabled = true;
            Frame.enabled = true;
        }
        
        private void DisableUIElements()
        {
            HealthSlider.gameObject.SetActive(false);
            HealthBackgroundSlider.gameObject.SetActive(false);
            PlayerName.enabled = false;
            Frame.enabled = false;
        }

        private void LateUpdate()
        {
            Assert.IsNotNull(_mainCamera);
            if (!_isInitalized) { return; }
            
            var pos = _mainCamera.WorldToScreenPoint(_character.gameObject.transform.position + PositionOffset);
            transform.position = Vector3.Lerp(transform.position, pos, _lerpSpeed * Time.deltaTime);
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName.text = playerName;
        }

        private async void OnHealthChanged(float previous, float current)
        {
            var newValue = current / _maxHealth;

            if (previous > current) {
                HealthSlider.value = newValue; 

                // Wait to change background -> Displays how much health was lost
                await Task.Delay(1000); 
            
                HealthBackgroundSlider.DOValue(newValue, 1f);
            }
            else {
                HealthBackgroundSlider.value = newValue; 

                // Wait to change foreground -> Displays how much health was gained
                await Task.Delay(1000); 
            
                HealthSlider.DOValue(newValue, 1f);
            }
            
            
        }

        private void OnDeath(float respawnTime)
        {
            this.gameObject.SetActive(false);
        }
        
        private void OnRespawn()
        {
            this.gameObject.SetActive(true);
        }
    }
}