using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

namespace MainProject.Scripts.Tools
{
    /// <summary>
    ///     Handles the load and error screens
    /// </summary>
    public class CanvasUtils : MonoBehaviour {
        public static CanvasUtils Instance;

        [SerializeField] private CanvasGroup _loader;
        [SerializeField] private float _fadeTime;
        [SerializeField] private TMP_Text _loaderText, _errorText;

        private TweenerCore<float, float, FloatOptions> _tween;

        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Toggle(false, instant: true);
        
            if(_cursorTexture != null && _clickedCursorTexture != null)
                SetCursor(_cursorTexture);
        }

        public void Toggle(bool on, string text = null, bool instant = false) {
            _loaderText.text = text;
            _loader.gameObject.SetActive(on);
            _tween?.Kill();
            _tween = _loader.DOFade(on ? 1 : 0, instant ? 0 : _fadeTime);
        }

        public void ShowError(string error) {
            _errorText.text = error;
            _errorText.DOFade(1, _fadeTime).OnComplete(() => { _errorText.DOFade(0, _fadeTime).SetDelay(1); });
        }

        #region Cursor

        [SerializeField] private Texture2D _cursorTexture, _clickedCursorTexture;
        private const CursorMode CursorMode = UnityEngine.CursorMode.Auto;
        private readonly Vector2 _hotSpot = Vector2.zero;
    
        private void Update() {
            //if (Input.GetMouseButtonDown(0)) SetCursor(_clickedCursorTexture);
            //else if (Input.GetMouseButtonUp(0)) SetCursor(_cursorTexture);
        }

        private void SetCursor(Texture2D tex) => Cursor.SetCursor(tex, _hotSpot, CursorMode);
    
        #endregion
    }

    public class Load : IDisposable {
        public Load(string text) {
            CanvasUtils.Instance.Toggle(true, text);
        }

        public void Dispose() {
            CanvasUtils.Instance.Toggle(false);
        }
    }
}