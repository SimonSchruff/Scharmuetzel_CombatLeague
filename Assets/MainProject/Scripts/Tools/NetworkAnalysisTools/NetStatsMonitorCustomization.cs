using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;

namespace MainProject.Scripts.Tools.NetworkAnalysisTools
{
    public class NetStatsMonitorCustomization : MonoBehaviour
    {
        private RuntimeNetStatsMonitor _monitor;
        private bool _isMonitorEnabled = false;
    
        private void Start()
        {
            _monitor = GetComponent<RuntimeNetStatsMonitor>(); 
            _monitor.enabled = false;
        }

        public void ToggleMonitor()
        {
            _isMonitorEnabled = !_isMonitorEnabled;
            _monitor.enabled = _isMonitorEnabled;
        }
    }
}
