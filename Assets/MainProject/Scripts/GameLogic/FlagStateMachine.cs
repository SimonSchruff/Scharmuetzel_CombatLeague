using System;
using MainProject.Scripts.DataStructures;
using UnityEngine;

namespace MainProject.Scripts.GameLogic
{
    public class FlagStateMachine : MonoBehaviour
    {
        public enum ThisFlagState
        {
            None, 
            Neutral,
            Charging, 
            Decharging,
            Paused,
            Captured,
        }
        
        
        
        /// <summary>
        /// The current state of the flag
        /// </summary>
        public ThisFlagState CurrentState => current_state;
        private ThisFlagState current_state;

        private int owner_player_amount;
        private int challenger_player_amount;

        private int current_points;
        private const int points_to_capture = 500;
        private const int points_per_tick = 1;

        private bool is_initalized = false;

        private void Initalize()
        {
            current_state = ThisFlagState.Neutral;
            is_initalized = true;
        }

        private void FixedUpdate()
        {
            if (!is_initalized) { return; }
            
            switch (current_state)
            {
                case ThisFlagState.None:
                case ThisFlagState.Neutral:
                case ThisFlagState.Paused:
                    return;
                    break;
                case ThisFlagState.Charging:
                    current_points += points_per_tick;
                    Evaluate_If_Captured();
                    break;
                case ThisFlagState.Decharging:
                    current_points -= points_per_tick;
                    Evaluate_If_Neutral();
                    break;
                case ThisFlagState.Captured:
                    // Give game points to game manager
                    break;
            }
        }

        private void Evaluate_If_Captured()
        {
            // return if flag is not owned yet
            if (current_points < points_to_capture) {
                return;
            }

            current_state = ThisFlagState.Captured;
            current_points = points_to_capture;
        }
        
        private void Evaluate_If_Neutral()
        {
            // return if flag is not neutral yet
            if (current_points > 0) {
                return;
            }

            current_state = ThisFlagState.Neutral;
            current_points = 0;
        }

        private void Evaluate_Player_Amount()
        {
            // Equal owners and challengers
            if (owner_player_amount == challenger_player_amount)
            {
                current_state = ThisFlagState.Paused;
            }

            // No challengers and more then 1 owner
            if (owner_player_amount > 0 && challenger_player_amount < 1)
            {
                // Already captured
                if (current_state == ThisFlagState.Captured) {
                    return;
                }
                
                current_state = ThisFlagState.Charging;
            }

            // No owner and at least one challenger
            if (owner_player_amount == 0 && challenger_player_amount > 0)
            {
                current_state = ThisFlagState.Decharging;
            }
            
            // At least 1 owner and one challenger
            if (owner_player_amount > 0 &&  challenger_player_amount > 0)
            {
                current_state = ThisFlagState.Paused;
            }
        }

        public void PlayerEntered(int teamID)
        {
            if (teamID == 1) {
                owner_player_amount++;
            }
            
            if (teamID == 2) {
                challenger_player_amount++;
            }
            
            Evaluate_Player_Amount();
        }

        public void PlayerExited(int teamID)
        {
            if (teamID == 1) {
                owner_player_amount--;
            }
            
            if (teamID == 2) {
                challenger_player_amount--;
            }
            
            Evaluate_Player_Amount();
        }
    }
}