using System;
using MainProject.Scripts.DataStructures;
using MainProject.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player
{
    public class PlayerWeapon : NetworkBehaviour
    {
        private Character _character;
        private BoxCollider _weaponCollider;
        private DamageType _dmg;

        private MeshRenderer _mr;
        
        private bool _isInitalized = false;

        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
        }

        public void Initalization(DamageType dmg, GameObject owner)
        {
            _dmg = dmg;
            _character = owner.GetComponent<Character>();
            _weaponCollider = this.gameObject.GetComponent<BoxCollider>();
            DisableCollider();
            _isInitalized = true;
        }
        
        
        private void OnTriggerEnter(Collider c)
        {
            if (!IsServer || !_isInitalized) { return; }
            
            // Check if player
            var pc = c.GetComponent<Character>(); 
            if (pc == null || pc.Net_TeamID.Value == _character.Net_TeamID.Value ) {
                // print($"Player {pc.OwnerClientId} of pc null or same team Id");
                return;
            }
            
            pc.LinkedHealth.Damage(_dmg, _character.gameObject);
        }

        public void SetMaterial(Material mat)
        {
            if (_mr == null) {
                _mr = GetComponent<MeshRenderer>();
            }
            
            _mr.material = mat;
        }

        public void EnableCollider()
        {
            _weaponCollider.enabled = true;
        }
        
        public void DisableCollider()
        {
            _weaponCollider.enabled = false;
        }
    }
}