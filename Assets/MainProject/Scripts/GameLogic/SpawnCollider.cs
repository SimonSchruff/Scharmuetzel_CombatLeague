using MainProject.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;

namespace MainProject.Scripts.GameLogic
{
    public class SpawnCollider : NetworkBehaviour
    {

        [SerializeField] private BoxCollider _startCollider;
        [SerializeField] private Material _inactiveMaterial;
        private Material _originalMaterial;
        private MeshRenderer[] _planeMeshRenderer;
    
        private NetworkVariable<bool> net_isOpen = new NetworkVariable<bool>();

        void Awake()
        {
            _planeMeshRenderer = GetComponentsInChildren<MeshRenderer>(); 
            _originalMaterial = _planeMeshRenderer[0].material;
            
            if(GameManager.Instance)
                GameManager.Instance.OnGameStarted += SetActive;
            else
            {
                print("NO GASME MANAGER");
            }
        }
        
        public override void OnNetworkSpawn()
        {
            SetInactive();

            if(net_isOpen.Value) {
                SetActive(0);
            }
            else {
                SetInactive();
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnGameStarted -= SetActive;
        }

        public void SetInactive()
        {
            _startCollider.enabled = true;
        
            foreach (var mr in _planeMeshRenderer)
            {
                mr.material = _inactiveMaterial;
            }

            if (IsServer) {
                net_isOpen.Value = false;
            }
        }
    
        public void SetActive(float time)
        {
            _startCollider.enabled = false;

            foreach (var mr in _planeMeshRenderer)
            {
                mr.material = _originalMaterial;
            }
            
            if (IsServer) {
                net_isOpen.Value = true;
            }
        }
    

    }
}
