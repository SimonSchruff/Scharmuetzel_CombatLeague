using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MainProject.Scripts.Player.PlayerUI
{
    public class MiniMap : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private RectTransform _mapPoint_1;
        [SerializeField] private RectTransform _mapPoint_2;
        [Space(10)]
        [SerializeField] private RectTransform playerMapPrefab;
        private Transform _worldPoint_1;
        private Transform _worldPoint_2;

        [Header("Settings")] 
        [SerializeField] private Color _team01Color;
        [SerializeField] private Color _team02Color;
        
        private Dictionary<Character, RectTransform> _mapPlayers = new Dictionary<Character, RectTransform>();
        private float _minimapRatio;

        public void UpdatePlayers(Dictionary<Character, HealthBar> players)
        {
            foreach (var rect in _mapPlayers.Values) {
                Destroy(rect.gameObject);
            }
            _mapPlayers.Clear();
            
            foreach (var p in players.Keys)
            {
                var mapPlayer = Instantiate(playerMapPrefab, Vector3.zero, Quaternion.identity);
                var img = mapPlayer.GetComponent<Image>();
                
                if (p.Net_TeamID.Value == 1) {
                    var color = _team01Color;
                    if (p.IsLocalPlayer) {
                        color.a = 255f;
                    }
                    img.color = color;
                }
                else if (p.Net_TeamID.Value == 2) {
                    var color = _team02Color;
                    if (p.IsLocalPlayer) {
                        color.a = 255f;
                    }
                    img.color = color;
                }
                
                mapPlayer.SetParent(this.gameObject.transform);
                _mapPlayers.Add(p, mapPlayer);
            }
            
            UpdatePlayerPos();
        }

        public void UpdatePlayerPos()
        {
            if(_mapPlayers.Count == 0 || _mapPlayers == null) { return; }
            
            foreach (var p in _mapPlayers)
            {
                p.Value.anchoredPosition =  _mapPoint_1.anchoredPosition + new Vector2((p.Key.transform.position.x - _worldPoint_1.position.x) * _minimapRatio, 
                    (p.Key.transform.position.z - _worldPoint_1.position.z) * _minimapRatio);
            }
        }

        public void CalculateMapRatio(Transform worldPoint_1, Transform worldPoint_2)
        {
            _worldPoint_1 = worldPoint_1;
            _worldPoint_2 = worldPoint_2;
            
            Vector3 distanceWorldVec = worldPoint_1.position - worldPoint_2.position;
            distanceWorldVec.y = 0f;
            float distWorld = distanceWorldVec.magnitude;

            float distMap = Mathf.Sqrt(
                Mathf.Pow((_mapPoint_1.anchoredPosition.x - _mapPoint_2.anchoredPosition.x), 2) +
                Mathf.Pow((_mapPoint_1.anchoredPosition.y - _mapPoint_2.anchoredPosition.y), 2)
            ); 
            
            _minimapRatio =  distMap / distWorld;
        }
    }
}
