using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    [System.Serializable]
    public class AbilityCooldown
    {
        public enum CooldownStates { Idle, Consuming, PauseOnEmpty, Refilling }
        /// if this is true, the cooldown won't do anything
        public bool Unlimited = false;
        /// the time it takes, in seconds, to consume the object
        public float ConsumptionDuration = 2f;
        /// the pause to apply before refilling once the object's been depleted
        public float PauseOnEmptyDuration = 1f;
        /// the duration of the refill, in seconds, if uninterrupted
        public float RefillDuration = 1f;
        /// whether or not the refill can be interrupted by a new Start instruction
        public bool CanInterruptRefill = true;
        
        /// the current state of the object
        public CooldownStates CooldownState = CooldownStates.Idle;
        /// the amount of duration left in the object at any given time
        public float CurrentDurationLeft;
        
        protected WaitForSeconds _pauseOnEmptyWFS;
        protected float _emptyReachedTimestamp = 0f;
        
        public virtual bool Ready()
        {
            if (Unlimited) {
                return true;
            }
            
            if (CooldownState == CooldownStates.Idle) {
                return true;
            }
            
            if ((CooldownState == CooldownStates.Refilling) && (CanInterruptRefill)) {
                return true;
            }
            
            return false;
        }

        public float Progress 
        {
            get
            {
                if (Unlimited) {
                    return 1f;
                }
                
                if (CooldownState == CooldownStates.Consuming || CooldownState == CooldownStates.PauseOnEmpty) {
                    return 0f;
                }
                
                if (CooldownState == CooldownStates.Refilling) {
                    return /*Mathf.Clamp01(*/CurrentDurationLeft / RefillDuration; /*)*/
                };
                
                return 1f; // refilled
            }
        }
        
        /// <summary>
        /// An init method that ensures the object is reset
        /// </summary>
        public virtual void Initialization()
        {
            _pauseOnEmptyWFS = new WaitForSeconds(PauseOnEmptyDuration);
            CurrentDurationLeft = ConsumptionDuration;
            CooldownState = CooldownStates.Idle;
            _emptyReachedTimestamp = 0f;
        }
        
        /// <summary>
        /// Starts consuming the cooldown object if possible
        /// </summary>
        public virtual void Start()
        {
            if (Ready()) {
                CooldownState = CooldownStates.Refilling;
            }
        }

        /// <summary>
        /// Stops consuming the object 
        /// </summary>
        public virtual void Stop()
        {
            if (CooldownState == CooldownStates.Consuming) {
                CooldownState = CooldownStates.PauseOnEmpty;
            }
        }

        /// <summary>
        /// Set cooldown to idle, so ability can be used 
        /// </summary>
        public virtual void SetCooldownReady()
        {
            CurrentDurationLeft = 0f;
            _emptyReachedTimestamp = Time.time;
            CooldownState = CooldownStates.Idle;
        }
        
        /// <summary>
        /// Processes the object's state machine
        /// </summary>
        public virtual void UpdateCooldown(bool isLocalPlayer)
        {
         
            if (Unlimited) {
                return;
            }
            
            switch (CooldownState)
            {
                case CooldownStates.Idle:
                    break;

                case CooldownStates.Consuming:
                    CurrentDurationLeft = CurrentDurationLeft - Time.deltaTime;
                    if (CurrentDurationLeft <= 0f) {
                        CurrentDurationLeft = 0f;
                        _emptyReachedTimestamp = Time.time;
                        CooldownState = CooldownStates.PauseOnEmpty;
                    }
                    break;

                case CooldownStates.PauseOnEmpty:
                    if (Time.time - _emptyReachedTimestamp >= PauseOnEmptyDuration) {
                        CooldownState = CooldownStates.Refilling;
                    }
                    break;

                case CooldownStates.Refilling:
                    // CurrentDurationLeft += (RefillDuration * Time.deltaTime) / RefillDuration;
                    if (isLocalPlayer) {
                        this.CurrentDurationLeft += Time.deltaTime / 2 ;
                    }
                    else {
                        this.CurrentDurationLeft += Time.deltaTime ;
                    }
                    
                    if (CurrentDurationLeft >= RefillDuration) {
                        CurrentDurationLeft = ConsumptionDuration;
                        CooldownState = CooldownStates.Idle;
                    }
                    break;
            }
            
        }
    }
}