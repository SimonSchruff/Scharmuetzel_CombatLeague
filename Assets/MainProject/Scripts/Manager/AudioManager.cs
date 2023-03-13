using System;
using MainProject.Scripts.DataStructures;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MainProject.Scripts.Manager
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;
    
        public SoundClip[] BackgroundMusicSoundClips;
        
        [Header("BackgroundMusic")]
        public AudioMixerGroup BackgroundMusicMixerGroup;

        private bool _isInitalized = false;
        private void Awake()
        {
            if (Instance == null) { Instance = this; } 
            else { Destroy(this); }

            foreach (var s in BackgroundMusicSoundClips)
            {
                s.Source = gameObject.AddComponent<AudioSource>();
                s.Source.clip = s.Clip;
            
                s.Source.volume = s.Volume;
                s.Source.pitch = s.Pitch;
            
                s.Source.loop = s.Loop;
                s.Source.outputAudioMixerGroup = BackgroundMusicMixerGroup;
            }
        
            _isInitalized = true;
        }

        private void Start()
        {
            PlayRandomBackgroundMusicClip();
        }

        private void Update()
        {
            bool isPlaying = false;
            foreach (var s in BackgroundMusicSoundClips) {
                if (s.Source.isPlaying) { isPlaying = true; }
            }

            if (isPlaying) {
                return;
            }
            
            PlayRandomBackgroundMusicClip();
        }

        private void PlayRandomBackgroundMusicClip()
        {
            SoundClip s = BackgroundMusicSoundClips[Random.Range(0, BackgroundMusicSoundClips.Length )];
            if(s == null) { return; }
            
            s.Source.Play();
        }
        
        /*
        public void Play(string soundClipName)
        {
            if (!_isInitalized) { return; }
        
            SoundClip soundClip = Array.Find(BackgroundMusicSoundClips, s => s.Name == soundClipName);
            if (soundClip == null) {
                Debug.LogWarning($"Could not find {soundClipName}!");
                return;
            }
        
            soundClip.Source.Play();
        }
        */
    }
}
