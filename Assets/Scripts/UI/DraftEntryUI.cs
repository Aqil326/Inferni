using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraftEntryUI : MonoBehaviour
{
    public enum EntryType
    {
        None,
        Card,
        Charm
    }

    [SerializeField]
    private CardUI cardUI;

    [SerializeField]
    private CharmUI charmUI;

    [SerializeField]
    private CanvasGroup canvasGroup;

    public string ID
    {
        get
        {
            switch(Type)
            {
                case EntryType.Card:
                    return cardUI.Card.Data.InternalID;
                case EntryType.Charm:
                    return charmUI.Charm.CharmData.InternalID;
                default:
                    return string.Empty;
            }
        }
    }

    public int Index { get; private set; }
    public EntryType Type { get; private set; }
    public bool IsHidden { get; private set; }
    public Action<DraftEntryUI> StartedDragEvent;
    public Action<DraftEntryUI> SelectedEvent;
    public Action<DraftEntryUI> PickedEvent;

    private bool isInteractable;

    public void Init(int index)
    {
        Index = index;
    }

    public Card GetCard()
    {
        return cardUI.Card;
    }

    public Charm GetCharm()
    {
        return charmUI.Charm;
    }

    public void SetCard(Card card)
    {
        Type = EntryType.Card;

        charmUI.gameObject.SetActive(false);
        cardUI.gameObject.SetActive(true);
        cardUI.SetCard(card);
        cardUI.SetXValue(CardXPreviewType.Initial);
    }

    public void SetCharm(Charm charm)
    {
        Type = EntryType.Charm;

        cardUI.gameObject.SetActive(false);
        charmUI.gameObject.SetActive(true);
        charmUI.SetCharm(charm);
    }

    public void OnClick(BaseEventData eventData)
    {
        if (!isInteractable)
        {
            return;
        }

        var pointerData = (PointerEventData)eventData;
        if (pointerData.clickCount == 1)
        {
            SelectedEvent?.Invoke(this);
        }
        else if(pointerData.clickCount == 2)
        {
            //Double click
            PickedEvent?.Invoke(this);

            //Clear tooltips
            ClearHover();
        }
    }

    public void OnStartDrag()
    {
        if(!isInteractable)
        {
            return;
        }
        StartedDragEvent?.Invoke(this);
    }

    public void Select()
    {
        switch(Type)
        {
            case EntryType.Card:
                cardUI.Select();
                break;
            case EntryType.Charm:
                charmUI.Select();
                break;
        }
    }

    public void Deselect()
    {
        switch (Type)
        {
            case EntryType.Card:
                cardUI.Deselect();
                break;
            case EntryType.Charm:
                charmUI.Deselect();
                break;
        }
    }

    public void Show(float alpha = 1, bool isInteractable = true)
    {
        canvasGroup.alpha = alpha;
        IsHidden = false;
        canvasGroup.blocksRaycasts = isInteractable;
        canvasGroup.interactable = isInteractable;
        this.isInteractable = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        IsHidden = true;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        ClearHover();
        isInteractable = false;
    }

    public void ClearHover()
    {
        switch (Type)
        {
            case EntryType.Card:
                cardUI.TryClearCardInspected();
                break;
            case EntryType.Charm:
                charmUI.TryClearTooltip();
                break;
        }
    }

    public void PlayCardFlipAnimation(float cardsAnimationDuration, bool showBack)
    {
        if (showBack)
        {
            cardUI.ShowBack();
        }
        else
        {
            cardUI.ShowFront();
        }
        cardUI.PlayFlipAnimation(null, cardsAnimationDuration);
    }
}
