using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [SerializeField]
    private SoundManager.Sound mouseoverSound;

    [SerializeField]
    private SoundManager.Sound activatedSound;

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseoverSound.Play(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        activatedSound.Play(false);
    }

}
