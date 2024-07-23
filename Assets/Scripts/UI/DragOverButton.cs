using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragOverButton :MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private GameObject selectedHighlight;

    public event Action<DragOverButton> PointerEnterEvent;
    public event Action<DragOverButton> PointerExitEvent;

    private void Start()
    {
        Deselect();
    }

    public void Select()
    {
        selectedHighlight.SetActive(true);
    }

    public void Deselect()
    {
        selectedHighlight.SetActive(false);
    }

    public void Activate()
    {
        button.onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent?.Invoke(this);
    }
}
