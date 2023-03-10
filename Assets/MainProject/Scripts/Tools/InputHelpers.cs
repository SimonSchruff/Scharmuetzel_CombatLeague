using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MainProject.Scripts.Tools
{
    public static class InputHelpers
    {
        /// <summary>
        /// Returns true if a controller is connected
        /// </summary>
        /// <returns></returns>
        public static bool CheckForController()
        {
            if (Gamepad.current == null) {
                return false;
            }
            else {
                Debug.Log("Connected Gamepad found: " + Gamepad.current.name);
                return true;
            }
        }
        
        /// <summary>
        /// Uses rumble if controller is connected; Needs to be reset with ResetCurrentHaptics();
        /// Compatability varies; 
        /// </summary>
        public static void SetControllerVibration(float leftMotor, float rightMotor)
        {
            if (Gamepad.current == null) { return ; }
            
            Gamepad.current.SetMotorSpeeds(leftMotor, rightMotor);

        }

        public static void ResetCurrentHaptics()
        {
            if (Gamepad.current == null) { return ; }
            
            Gamepad.current.ResetHaptics(); 
        }
    }
}