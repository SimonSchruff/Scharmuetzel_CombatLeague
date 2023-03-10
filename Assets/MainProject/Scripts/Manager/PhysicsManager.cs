using System.Collections;
using System.Collections.Generic;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainProject.Scripts.Manager
{
    public class PhysicsManager : NetworkSingleton<PhysicsManager>
    {
        
        private Scene _gameScene;
        private PhysicsScene _physicsScene;
 
        void Awake()
        {
            // Physics.autoSyncTransforms = false;

            _gameScene = SceneManager.GetActiveScene();
            _physicsScene = _gameScene.GetPhysicsScene();
            
            Physics.autoSimulation = false;

        }

        /// <summary>
        /// Steps the physics scene by specified deltaTime and syncs transforms;
        /// </summary>
        public void StepPhysicsScene(float dt)
        {
            _physicsScene.Simulate(dt);
            
            // Suggested to fix CharacterController.Move() not working in rewind; -> Does nothing :(
            // https://forum.unity.com/threads/calling-charactercontroller-move-more-than-one-time-per-frame.958813/
            // Physics.SyncTransforms();
        }

        public void SyncTransforms()
        {
            Physics.SyncTransforms();
        }
    }
}


