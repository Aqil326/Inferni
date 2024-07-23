using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NemesisIconUI : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    private void Start()
    {
        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, OnCardDragStarted);
        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, OnCardDragEnded);
    }

   private void OnDestroy()
    {
        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, OnCardDragStarted);
        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, OnCardDragEnded);
    }

    public void ShowNemesisTooltip()
    {
        EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, new TooltipData()
        {
            Title = GlobalGameSettings.Settings.NemesisTitle,
            Description = GlobalGameSettings.Settings.NemesisDescription.Replace("{X}", GlobalGameSettings.Settings.NemesisDefeatRPGain.ToString())
        });
    }

    public void ClearNemesisTooltip()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);
    }

    private void OnCardDragStarted(Card card)
    {
        canvasGroup.blocksRaycasts = false;
    }

    private void OnCardDragEnded(Card card)
    {
        canvasGroup.blocksRaycasts = true;
    }
}
