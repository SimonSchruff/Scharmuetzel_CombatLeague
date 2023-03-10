using MainProject.Scripts.DataStructures;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.Player.CharacterAbilities
{
    public class OneDirectionProjectile : NetworkBehaviour
    {
        [SerializeField] private GameObject _hitFX;
        [SerializeField] private bool _destroyOnCollision = true;
        public E_DestructionMode DestructionMode = E_DestructionMode.Time;
        
        // To do hostile check on hit
        private int _ownerTeamId;
        // Settings
        private DamageType _dmg;
        private float _moveSpeed = 4f;
        private float _lifeTime = 10f;

        private float _timer = 0f;
        private bool _isInitalized = false;
        private Vector3 _moveDir;

        public enum E_DestructionMode
        {
            Time, 
            Distance
        }
        
        public void Initalize(int teamId, Vector3 position, Vector3 direction, DamageType dmg, float moveSpeed, float lifeTime)
        {
            transform.position = position;

            _ownerTeamId = teamId;
            _moveSpeed = moveSpeed;
            _lifeTime = lifeTime;
            _moveDir = direction * _moveSpeed;
            _dmg = dmg;
            
            _isInitalized = true;
        }

        private void FixedUpdate()
        {
            if (!IsServer || !_isInitalized) {
                return;
            }

            switch (DestructionMode)
            {
                case E_DestructionMode.Time:
                    _timer += Time.fixedDeltaTime;
                    if (_lifeTime <= _timer) {
                        DestroyProjectile();
                        return;
                    }
                    break;
                case E_DestructionMode.Distance:
                    Debug.LogWarning("Distance Mode Not Implemented!");
                    break;
            }

            transform.position += _moveDir * Time.fixedDeltaTime;
        }

        private void OnTriggerEnter(Collider coll)
        {
            if (!IsServer || !_isInitalized) {
                return;
            }

            print("Coll Projectile");
            
            
            var character = coll.GetComponent<Character>();
            if (character != null) {
                // Hostile check
                if (IsHostileCheck(character)) {
                    // Do damage
                    character.LinkedHealth.Damage(_dmg, this.gameObject, 0f);
                }
            }

            if (_destroyOnCollision) {
                DestroyProjectile();
            }
            
            Debug.Log($"Obj {coll.gameObject.name} hit!");
        }

        private bool IsHostileCheck(Character character)
        {
            if (!IsServer) { return false; }
            
            if (_ownerTeamId == character.Net_TeamID.Value) {
                return false;
            }

            return true;
        }

        private void DestroyProjectile()
        {
            
            Destroy(this.gameObject);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            SpawnHitFX();
        }

        private void SpawnHitFX()
        {
            var fx = Instantiate(_hitFX, transform.position, transform.rotation); 
        }
    }
}
