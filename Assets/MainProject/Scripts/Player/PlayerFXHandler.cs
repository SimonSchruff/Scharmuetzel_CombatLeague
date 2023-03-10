using System;
using System.Collections;
using MainProject.Scripts.DataStructures;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class PlayerFXHandler : MonoBehaviour
    {
        [Header("Spawn/Despawn FX")] [SerializeField]
        private ParticleSystem SpawnFX_Team01;

        [SerializeField] private ParticleSystem SpawnFX_Team02;

        [Header("Weapon FX")] [SerializeField] private GameObject WeaponTrailFXObj;

        [Header("Ability FX")] [SerializeField]
        private GameObject GroundSpikeFXObject;

        [SerializeField] private GameObject DragonFXObj;

        [Header("Dash Trail FX")] [SerializeField]
        private float _trailMeshRefreshRate;

        [SerializeField] private float _trailMeshDestroyDelay;
        [SerializeField] private Material _trailMeshMaterialTeam01;
        [SerializeField] private Material _trailMeshMaterialTeam02;

        [Header("Healing")] public GameObject HealPotionFXObject;
        public ParticleSystem HealFX;

        [Header("Hit FLash FX")] 
        [SerializeField] private float _flashDuration = 0.2f;
        [SerializeField] private Material _hitFlashMaterialTeam01;
        [SerializeField] private Material _hitFlashMaterialTeam02;
        private Material _originalMaterial;

        private int _teamID;


        private SkinnedMeshRenderer[] _skinnedMRs;
        private Character _character;
        private Health _health;

        private void Awake()
        {
            _skinnedMRs = GetComponentsInChildren<SkinnedMeshRenderer>();
            _character = this.GetComponent<Character>();
            
        }
        
        private void Start()
        {
            _health = _character.LinkedHealth;
            _health.Net_CurrentHealth.OnValueChanged += PlayHitFlashFX;

        }

        private void OnDestroy()
        {
            _health.Net_CurrentHealth.OnValueChanged += PlayHitFlashFX;

        }

        public void PlaySpawnFX(int teamID)
        {
            _teamID = teamID;

            switch (teamID)
            {
                case 1:
                    SpawnFX_Team01.Play();
                    _originalMaterial = _skinnedMRs[0].material;
                    break;
                case 2:
                    SpawnFX_Team02.Play();
                    _originalMaterial = _skinnedMRs[0].material;

                    break;
            }
        }

        public void PlayGroundSpikeFX()
        {
            var obj = Instantiate(GroundSpikeFXObject, transform.position, transform.rotation);
        }

        public void PlayDragonPunchFX()
        {
            var spawnPos = transform.position + transform.forward * 3f + Vector3.up;
            var obj = Instantiate(DragonFXObj, spawnPos, transform.rotation);
        }

        public void PlayHitFlashFX(float prev, float current)
        {
            if (current < prev) {
                StartCoroutine(HitFlashCo());
            }
        }

        private IEnumerator HitFlashCo()
        {
            yield return new WaitForSeconds(0.1f);
            
            // Set materials of _skinnedMRs to hit flash material   
            for (int i = 0; i < _skinnedMRs.Length; i++)
            {
                _skinnedMRs[i].material = _teamID == 1 ? _hitFlashMaterialTeam01 : _hitFlashMaterialTeam02;
            }
            
            yield return new WaitForSeconds(_flashDuration);
            
            // Set materials of _skinnedMRs to original material
            for (int i = 0; i < _skinnedMRs.Length; i++)
            {
                _skinnedMRs[i].material = _originalMaterial;
            }
        }

        
        #region  WEAPON_TRAIL
        
        public void EnableWeaponTrailForTime(float seconds)
        {
            StartCoroutine(EnableWeaponTrailForTimeCo(seconds)); 
        }

        private IEnumerator EnableWeaponTrailForTimeCo(float seconds)
        {
            EnableWeaponTrailFX();
            yield return new WaitForSeconds(seconds); 
            DisableWeaponTrailFX();
        }
        
        public void EnableWeaponTrailFX()
        {
            WeaponTrailFXObj.SetActive(true);
        }

        public void DisableWeaponTrailFX()
        {
            WeaponTrailFXObj.SetActive(false);
        }

        public void StartPlayTrailFX(float timeActive, int teamID)
        {
            StartCoroutine(TrailFX(timeActive, teamID));
        }

        private IEnumerator TrailFX(float timeActive, int teamID)
        {
            while (timeActive >= 0f)
            {
                timeActive -= _trailMeshRefreshRate;

                for (int i = 0; i < _skinnedMRs.Length; i++)
                {
                    GameObject obj = new GameObject(); 
                    obj.transform.SetPositionAndRotation(_character.CharacterModel.transform.position, transform.rotation);

                    MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                    MeshFilter mf = obj.AddComponent<MeshFilter>();

                    Mesh mesh = new Mesh(); 
                    _skinnedMRs[i].BakeMesh(mesh);

                    mf.mesh = mesh;
                    mr.material = teamID == 1 ? _trailMeshMaterialTeam01 : _trailMeshMaterialTeam02;

                    Destroy(obj, _trailMeshDestroyDelay);
                }

                yield return new WaitForSeconds(_trailMeshRefreshRate);
            }
        }

        #endregion
        
        #region HEALING
        public void EnableHealPotionLight()
        {
            HealPotionFXObject.SetActive(true);
        }
        
        public void DisableHealPotionLight()
        {
            HealPotionFXObject.SetActive(false);
        }

        public void PlayHealFX()
        {
            HealFX.Play();
        }
        #endregion
    }
}