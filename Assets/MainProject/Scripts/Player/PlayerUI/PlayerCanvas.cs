using System;
using System.Collections;
using DG.Tweening;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.DataStructures.PlayerData;
using MainProject.Scripts.Manager;
using MainProject.Scripts.Tools;
using MainProject.Scripts.Tools.Services;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace MainProject.Scripts.Player.PlayerUI
{
    public class PlayerCanvas : MonoBehaviour
    {
        [Header("Screen Objects")]
        public GameObject InGameHUD;
        public GameObject GameOverHUD;
        public GameObject MenuHUD;
        
        [Header("Ability Images")]
        public Image _cast01Img; 
        public Image _cast02Img, _cast03Img, _cast04Img, _dashImg, _healImg, _basicAttackImg;
        public Material _spriteFlashMat;

        private float _ability01Cooldown, _ability02Cooldown, _ability03Cooldown, 
            _ability04Cooldown, _dashCooldown, _healCooldown, _basicAttackCooldown;

        [Header("On Death UI")]
        public TextMeshProUGUI RespawnTimer;

        [Header("Text")]
        public TextMeshProUGUI LobbyCode;
        public TextMeshProUGUI GameTime;
        public TextMeshProUGUI HealthNumberText;


        [Header("Flag State Display")]
        public Image Flag01Img;
        public Image Flag02Img;
        public Image Flag03Img;
        [SerializeField] private Color _team01Color;
        [SerializeField] private Color _team02Color;
        
        
        [Header("Slider")]
        public Slider HealthSlider;
        public Slider HealthBackgroundSlider;
        public Slider Team01PointsSlider;
        public Slider Team02PointsSlider;

        [Header("Menu HUD")]
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        [SerializeField] private TextMeshProUGUI _lobbyCodeText;
        [SerializeField] private TextMeshProUGUI _pingText;
        
        [Header("Game Over HUD")]
        public TextMeshProUGUI TeamWonText;

        private float _startGameTime;
        
        
        private bool _isInitialized;


        public void Init( )
        {
            GameManager.Instance.OnTeamWon += EnableGameOverHud;
            _startGameTime = GameManager.Instance.GetLocalCurrentGameTime();
            
            InGameHUD.SetActive(true);
            MenuHUD.SetActive(false);
            GameOverHUD.SetActive(false);
            
            DisableRespawnTimerText();
            
            SetGameTimeText(_startGameTime);
            
            _isInitialized = true;
        }

        private void OnDisable()
        {
            if (!_isInitialized) {
                return;
            }
            
            GameManager.Instance.OnTeamWon -= EnableGameOverHud;
        }


        private void Update()
        {
            
            if (!_isInitialized) {
                return;
            }

            _startGameTime += Time.deltaTime;
            SetGameTimeText(_startGameTime);    
        }

        private void SetGameTimeText(float gameTime)
        {
            TimeSpan time = new TimeSpan(0, 0, (int) gameTime);
            GameTime.text = $"{time.TotalMinutes:00}:{time.Seconds:00}";
        }
        
        public void QuitGame()
        {
            Debug.LogWarning("Application was quit!");
            Application.Quit();
        }

        public void OnBackToMenu()
        {
            OnLobbyLeft();
        }
        
        private async void OnLobbyLeft() {
            using (new Load("Leaving Lobby...")) {
                
                if (NetworkSaveManager.Instance != null) {
                    NetworkSaveManager.Instance.ClearPlayerData();
                }
                
                NetworkManager.Singleton.Shutdown();
                await MatchmakingService.LeaveLobby();
                
                Authentication.Logout();

                SceneManager.LoadScene("AuthReload", LoadSceneMode.Single);
            }
            
        }

        public void EnableGameOverHud(int teamId)
        {
            InGameHUD.SetActive(false);
            MenuHUD.SetActive(false);
            GameOverHUD.SetActive(true);
            TeamWonText.text = $"Team {teamId} won!"; 
        }
        
        public void EnableMenuHUD()
        {
            MenuHUD.SetActive(true);
        }
        
        public void DisableMenuHUD()
        {
            MenuHUD.SetActive(false);
        }

        public void InitMenuHUDInfo(string lobbyCode, string lobbyName)
        {
            _lobbyNameText.text = lobbyName;
            _lobbyCodeText.text = $"Lobby Code: {lobbyCode}";
        }
      

        public void StartAbilityCooldown(AbilityTypes ability)
        {
            float time = 0f;
            Image abilityImg = null;
            switch (ability)
            {
                case AbilityTypes.BasicAttack:
                    abilityImg = _basicAttackImg;
                    time = _basicAttackCooldown;
                    break;
                
                case AbilityTypes.BasicRanged:
                    abilityImg = _cast01Img;
                    time = _ability01Cooldown;
                    break;
                case AbilityTypes.GroundSpikes:
                    abilityImg = _cast02Img;
                    time = _ability02Cooldown;
                    break;
                case AbilityTypes.SpinAttack:
                    abilityImg = _cast03Img;
                    time = _ability03Cooldown;
                    break;
                case AbilityTypes.DragonPunch:
                    abilityImg = _cast04Img;
                    time = _ability04Cooldown;
                    break;
                    
                case AbilityTypes.Dash:
                    abilityImg = _dashImg;
                    time = _dashCooldown;
                    break;
                case AbilityTypes.HealPotion:
                    abilityImg = _healImg;
                    time = _healCooldown;
                    break;
                default:
                    Debug.LogWarning($"Ability {ability} not set up! Cannot start cooldown!");
                    return;
                    break;
            }

            if (abilityImg == null) {
                return;
            }

            StartCoroutine(AbilityUICooldownTimer(abilityImg, time));
        }


        private bool _abilityReset = false;
        
        private IEnumerator AbilityUICooldownTimer(Image abilityImg, float cooldownTime)
        {
            _abilityReset = false;
            float timer = 0f;
            
            while (timer < cooldownTime)
            {
                timer += Time.deltaTime;
                abilityImg.fillAmount = timer / cooldownTime;

                if (!_abilityReset) {
                    yield return new WaitForEndOfFrame();
                }
                else {
                    _abilityReset = false;
                    yield break;
                }
            }
            
            // Cooldown finished
            abilityImg.fillAmount = 1f;
            StartCoroutine(FlashAbilitySpriteWhenReady(abilityImg));
        }

        private IEnumerator FlashAbilitySpriteWhenReady(Image abilityImg)
        {
            if(_spriteFlashMat == null) {
                yield break;
            }
            
            abilityImg.material = _spriteFlashMat;
            yield return new WaitForSeconds(0.2f);
            abilityImg.material = null;
        }

        /// <summary>
        /// Set value of fill on all ability images
        /// </summary>
        /// <param name="valueF">Float value from 0-1f</param>
        public void SetAbilityImagesByValue(float valueF)
        {
            _abilityReset = true;

            _dashImg.fillAmount = valueF;
            _healImg.fillAmount = valueF;
            _basicAttackImg.fillAmount = valueF;
            _cast01Img.fillAmount = valueF;
            _cast02Img.fillAmount = valueF;
            _cast03Img.fillAmount = valueF;
            _cast04Img.fillAmount = valueF;
        }

        public void SetCooldownTime(AbilityTypes ability, float time)
        {
            switch (ability)
            {
                case AbilityTypes.BasicAttack:
                    _basicAttackCooldown = time;
                    break;
                case AbilityTypes.BasicRanged:
                    _ability01Cooldown = time;
                    break;
                case AbilityTypes.GroundSpikes:
                    _ability02Cooldown = time;
                    break;
                case AbilityTypes.SpinAttack:
                    _ability03Cooldown = time;
                    break;
                case AbilityTypes.DragonPunch:  
                    _ability04Cooldown = time;
                    break;
                case AbilityTypes.Dash:
                    _dashCooldown = time;
                    break;
                case AbilityTypes.HealPotion:
                    _healCooldown = time;
                    break;
                default:
                    Debug.LogWarning($"Ability {ability} not set up! Cannot set cooldown!");
                    return;
                    break;
            }
        }

        public async void SetHealthSlider(float value, bool healthLost)
        {
            if (healthLost) {
                HealthSlider.value = value;
                await Task.Delay(1000);
                HealthBackgroundSlider.DOValue(value, 1f);
            }
            else {
                HealthBackgroundSlider.value = value;
                await Task.Delay(1000);
                HealthSlider.DOValue(value, 1f);
            }
        }

        public void ResetHealthSlider()
        {
            HealthSlider.value = 1f;
            HealthBackgroundSlider.value = 1f;
        }

        public void SetHealthBarText(float currentHealth, float maxHealth = 1000)
        {
            HealthNumberText.text = $"{currentHealth}/1000";

        }
        
        public void SetPointSlider(int teamId, float value)
        {
            if (teamId == 1)
            {
                Team01PointsSlider.value = value;
            }
            else if (teamId == 2)
            {
                Team02PointsSlider.value = value;
            }
        }

        public void SetLobbyCodeText(string text)
        {
            LobbyCode.text = $"Lobby Code: {text}";
        }

        public void UpdateFlagImages(int flagId, FlagState state)
        {
            switch (flagId)
            {
                case 1: 
                    SetFlagImageState(ref Flag01Img, state);
                    break;
                case 2: 
                    SetFlagImageState(ref Flag02Img, state);
                    break;
                case 3: 
                    SetFlagImageState(ref Flag03Img, state);
                    break;
                
            }
        }

        private void SetFlagImageState(ref Image img, FlagState state)
        {
            switch (state)
            {
                case FlagState.Neutral:
                    img.color = Color.white;
                    break;
                case FlagState.Team01Owned:
                    img.color = _team01Color;
                    break;
                case FlagState.Team01Charging:
                case FlagState.Team01Decharging:
                    img.color = Color.Lerp(_team01Color, Color.white, 0.5f);
                    break;
                case FlagState.Team02Owned:
                    img.color = _team02Color;
                    break;
                case FlagState.Team02Charging:
                case FlagState.Team02Decharging:
                    img.color = Color.Lerp(_team02Color, Color.white, 0.5f);
                    break;
                
            }
        }

        public void EnableRespawnTimerText()
        {
            RespawnTimer.gameObject.SetActive(true);
        }
        
        public void DisableRespawnTimerText()
        {
            RespawnTimer.gameObject.SetActive(false);
        }

        public void SetRespawnTimerText(float time)
        {
            RespawnTimer.text = String.Format("{0:00.0}", time);
        }
    }
}