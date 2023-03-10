using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Player;
using MainProject.Scripts.Tools;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.GameLogic
{
    public class DamageTrigger : NetworkBehaviour
    {
        public DamageType Damage;

        private void OnTriggerEnter(Collider c)
        {
            if (!IsServer) { return; }
            
            // Check if player
            var health = c.GetComponent<Health>(); 
            if (health == null) {
                return;
            }

            DamageCharacter(health);
        }
    
        private void OnTriggerStay(Collider c)
        {
            if (!IsServer) { return; }
            
            // Check if player
            var health = c.GetComponent<Health>(); 
            if (health == null) {
                return;
            }

            DamageCharacter(health);
        }



        private void DamageCharacter(Health h)
        {
            h.Damage(Damage, this.gameObject);
        }
    }
}
