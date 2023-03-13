using UnityEngine;
using UnityEngine.Audio;


namespace MainProject.Scripts.DataStructures
{
    [System.Serializable]
    public class SoundClip
    {
        public string Name;
        public AudioClip Clip;
        public bool Loop;
        
        [Range(0f, 1f)] public float Volume;
        [Range(0f, 1f)] public float Pitch;
        
        
        [HideInInspector] public AudioSource Source;

    }
}