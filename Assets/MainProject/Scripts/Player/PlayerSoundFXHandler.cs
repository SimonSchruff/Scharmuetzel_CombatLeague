using System;
using MainProject.Scripts.DataStructures;
using UnityEngine;
using UnityEngine.Audio;

namespace MainProject.Scripts.Player
{
    public class PlayerSoundFXHandler : MonoBehaviour
    {
        
        [Header("SFX Clips")]
        public SoundClip[] SoundClips;
        public AudioMixerGroup AbilitiesMixerGroup;

        [Header("SFX Settings")] 
        [SerializeField] private float _minDist = 0.5f;
        [SerializeField] private float _maxDist = 10f; 
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Linear;
        private float _spatialBlend = 1f;

        
        [Header("Foot Step SFX")]
        public SoundClip[] FootstepSoundClips;
        public AudioMixerGroup FootstepsMixerGroup;
        [SerializeField] private float _footstepSoundInterval = 0.5f;

        private bool _footstepSoundEnabled = false;
        private bool _isInitalized = false;

        private float _timer = 0f;

        private void Awake()
        {
            SetUpClips(ref SoundClips, AbilitiesMixerGroup);
            SetUpClips(ref FootstepSoundClips, FootstepsMixerGroup);

            _isInitalized = true;
        }

        private void SetUpClips(ref SoundClip[] clips, AudioMixerGroup mixerGroup)
        {
            foreach (var s in clips)
            {
                s.Source = gameObject.AddComponent<AudioSource>();
                s.Source.clip = s.Clip;
                s.Source.outputAudioMixerGroup = mixerGroup;

                s.Source.volume = s.Volume;
                s.Source.pitch = s.Pitch;
                
                s.Source.spatialBlend = _spatialBlend;
                s.Source.rolloffMode = _rolloffMode;
                s.Source.minDistance = _minDist;
                s.Source.maxDistance = _maxDist;
            
                s.Source.loop = s.Loop; 
            }
        }

        private void Update()
        {
            if (_footstepSoundEnabled)
            {
                _timer += Time.deltaTime;
                if(_timer > _footstepSoundInterval)
                {
                    _timer = 0f;
                    PlayRandomFootstepSoundClip();
                }
            }
        }

        public void Play(string soundClipName)
        {
            if (!_isInitalized) { return; }
        
            SoundClip soundClip = Array.Find(SoundClips, s => s.Name == soundClipName);
            if (soundClip == null) {
                Debug.LogWarning($"Could not find {soundClipName}!");
                return;
            }
        
            soundClip.Source.Play();
        }
        
        private void PlayRandomFootstepSoundClip()
        {
            SoundClip sound = FootstepSoundClips[UnityEngine.Random.Range(0, FootstepSoundClips.Length )];
            if (sound == null || !_isInitalized) { return; }
            
            sound.Source.Play();
        }

        public void EnableFootstepSfx()
        {
            _footstepSoundEnabled = true;
        }
        
        public void DisableFootstepSfx()
        {
            _footstepSoundEnabled = false;
        }
    }
}
