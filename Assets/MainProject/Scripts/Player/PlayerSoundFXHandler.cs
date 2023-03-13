using System;
using MainProject.Scripts.DataStructures;
using UnityEngine;
using UnityEngine.Audio;

namespace MainProject.Scripts.Player
{
    public class PlayerSoundFXHandler : MonoBehaviour
    {
        public SoundClip[] SoundClips;

        
        private bool _isInitalized = false;

        private void Awake()
        {
            foreach (var s in SoundClips)
            {
                s.Source = gameObject.AddComponent<AudioSource>();
                s.Source.clip = s.Clip;
            
                s.Source.volume = s.Volume;
                s.Source.pitch = s.Pitch;
            
                s.Source.loop = s.Loop; 
            }
        
            _isInitalized = true;
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
    }
}
