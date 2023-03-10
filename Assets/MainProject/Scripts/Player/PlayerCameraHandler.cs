using Cinemachine;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MainProject.Scripts.Player
{
    public class PlayerCameraHandler : NetworkBehaviour
    {
        [Tooltip("The camera that follows the local client")]
        public CinemachineVirtualCamera FollowCamera;
        [Tooltip("An object to use as the camera's point of focus and follow target")]
        public GameObject CameraTarget;
        
        private Volume _ppVolume;
        private ColorAdjustments _colorAdjustments;

        private void Awake()
        {
            //if (Instance == null) { Instance = this;}
            //else {Destroy(this);}
            
            // Get PP Volume
            if (!IsLocalPlayer) {
                return;
            }


            _ppVolume = FindObjectOfType<Volume>();
            if (_ppVolume == null) {
                print("Could not find PP Volume");
            }
            
            // Get Color Adjustments
            if (_ppVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments adjustments)) {
                _colorAdjustments = adjustments;
            }
            else {
                print("Could not get ColorAdjustments");
            }
            
        }

        public void SetUpFollowPlayerCamera()
        {
            Assert.IsNotNull(FollowCamera);

            // instantiate camera target
            if (CameraTarget == null) {
                var character = this.GetComponent<Character>();
                CameraTarget = character.CharacterModel;
            }
            
            // Set player as follow target of vcam
            var cam = Instantiate(FollowCamera);
            cam.Follow = CameraTarget.transform;
        }

        public void SetBW(bool enabled)
        {
            if (_ppVolume == null)
            {
                _ppVolume = FindObjectOfType<Volume>();
                if (_ppVolume == null) {
                    print("Could not find PP Volume");
                    return;
                }

            }
            
            if (_colorAdjustments == null )
            {
                if (_ppVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments adjustments)) {
                    _colorAdjustments = adjustments;
                }
                else {
                    print("Could not get ColorAdjustments");
                    return;
                }
            }
            
            
            if (enabled) {
                _colorAdjustments.saturation.value = -100f;
                _colorAdjustments.contrast.value = 50f;
            }
            else {
                _colorAdjustments.saturation.value = 0f;
                _colorAdjustments.contrast.value = 0f;
            }
        }
    }
}
