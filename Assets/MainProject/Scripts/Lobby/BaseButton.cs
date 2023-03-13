using UnityEngine;
using UnityEngine.EventSystems;

public class BaseButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    
    private AudioSource _source;

    
    void Awake()
    {
        _source = GetComponent<AudioSource>();

    }
    
    private void HighlightButton()
    {
        _source.Play();
    }

    public void OnSelect(BaseEventData eventData)
    {
        HighlightButton();

    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        HighlightButton();

    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        HighlightButton();

    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        HighlightButton();

    }
}
