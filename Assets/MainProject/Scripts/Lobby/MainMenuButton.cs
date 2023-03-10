
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{

    [Header("Settings")]
    [SerializeField] private float _textSize; 
    [SerializeField] private float _textSizeHighlight; 
    
    [Header("References")]
    [SerializeField] private TMP_FontAsset _font; 
    [SerializeField] private TMP_FontAsset _fontHighlight; 
    [SerializeField] private Color _color; 
    [SerializeField] private Color _colorHighlight; 
    
    private Button _button;
    private TextMeshProUGUI _buttonText;


    private void Awake()
    {
        _button = GetComponent<Button>();
        _buttonText = GetComponentInChildren<TextMeshProUGUI>();

        _buttonText.font = _font;
        _buttonText.fontSize = _textSize;
        _buttonText.color = _color;
    }


    private void HighlightButton()
    {
        _buttonText.font = _fontHighlight;
        _buttonText.color = _colorHighlight;
        _buttonText.fontSize = _textSizeHighlight;
    }

    private void ResetHighlightButton()
    {
        _buttonText.font = _font;
        _buttonText.color = _color;
        _buttonText.fontSize = _textSize;
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected");
        HighlightButton();

    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("Deselected");
        ResetHighlightButton();
    }
    
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        HighlightButton();
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        ResetHighlightButton(); 
    }
}
