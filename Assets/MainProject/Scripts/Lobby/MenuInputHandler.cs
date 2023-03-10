using System.Collections;
using MainProject.Scripts.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MainProject.Scripts.Lobby
{
    public class MenuInputHandler : MonoBehaviour
    {
        private int _buttonCheckTries = 0;
        
        void Start()
        {
            SelectFirstButtonInScene();
        }


        private void SelectFirstButtonInScene()
        {
            if (!InputHelpers.CheckForController()) { return;}

            var buttons = FindObjectsOfType<Button>();
            
            if(buttons.Length == 0) {
                _buttonCheckTries++;
                if (_buttonCheckTries < 5) {
                    StartCoroutine(TryCheckForButtons());
                }
                return; 
            }
            
            buttons[0].Select();
            
            print("Button Select");
        }

        private IEnumerator TryCheckForButtons()
        {
            yield return new WaitForSeconds(0.5f);
            SelectFirstButtonInScene();
        }

        public void SelectButtonInScene()
        {
            _buttonCheckTries = 0;
            SelectFirstButtonInScene();
        }
        
    }
}
