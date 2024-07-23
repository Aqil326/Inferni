using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardTooltipUI: MonoBehaviour
{
    [SerializeField]
    private GameObject cardTooltip;

    [SerializeField]
    private RectTransform backgroundRectTransform;

    [SerializeField]
    private RectTransform canvasRectTransform;

    [SerializeField]
    private TextMeshProUGUI cardName;

    [SerializeField]
    private TextMeshProUGUI cardDescription;

    [SerializeField]
    private CastCostUI[] castCostUIs;

    [SerializeField]
    private GameObject castingTimeParent;

    [SerializeField]
    private TextMeshProUGUI castingTime;

    [SerializeField]
    private Image xValueParent;

    [SerializeField]
    private TextMeshProUGUI xValue;

    [SerializeField]
    private Image targetIcon;

    [SerializeField]
    private TextMeshProUGUI targetText;

    [SerializeField]
    private RectTransform keywordUIParent;

    [SerializeField]
    private TooltipUI tooltipUIPrefab;

    [SerializeField]
    private CardTooltipUI cardTooltipUIPrefab;

    [SerializeField]
    private bool isMainCardTootltip = true;

    private List<TooltipUI> keywordTooltipUIs = new List<TooltipUI>();
    private List<CardTooltipUI> subCardTooltipUIs = new List<CardTooltipUI>();
    private bool isShowingPreview;
    private CardInspectedData cardInspectedData;
    private List<CardInspectedData> inspectedCards = new List<CardInspectedData>();
    private bool isInFlight;
    private RectTransform rectTransform; 

    private void Awake()
    {
        if (isMainCardTootltip)
        {
            EventBus.StartListening<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, SetCard);
            EventBus.StartListening<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, RemoveCard);
            ClearPreview();
        }
        rectTransform = GetComponent<RectTransform>();
        
    }

    private void SetCard(CardInspectedData data)
    {
        inspectedCards.Add(data);
        ShowCardTooltip(data);
    }

    public void ShowCard(Card card)
    {
        cardTooltip.SetActive(true);
        cardName.text = card.Data.Name;
        cardDescription.text = card.GetCardText();

        card.SetCastCostUIs(castCostUIs);

        castingTimeParent.SetActive(card.GetCardCastingTime() != 0);
        castingTime.text = card.GetCardCastingTime() + "s";

        var icon = GlobalGameSettings.Settings.GetTargetIcon(card.Data.Target);
        if (targetIcon != null)
        {
            targetIcon.sprite = icon.sprite;
            targetText.text = icon.targetText;
        }

        List<TooltipData> keywordDatas = new List<TooltipData>();
        List<CardEffect> list = card.GetAllCardEffects();

        int tooltipIndex = 0;
        int subCardIndex = 0;

        for (int i = 0; i < list.Count; i++)
        {
            CardEffect effect = list[i];
            if (effect.HasKeyword(out var keyword))
            {
                TooltipUI keywordTooltipUI = null;
                if (keywordTooltipUIs.Count <= tooltipIndex)
                {
                    keywordTooltipUI = Instantiate(tooltipUIPrefab, keywordUIParent);
                    keywordTooltipUIs.Add(keywordTooltipUI);
                }
                else
                {
                    keywordTooltipUI = keywordTooltipUIs[tooltipIndex];
                }
                keywordTooltipUI.Show(keyword);
                tooltipIndex++;
            }

            if (effect.HasSubCard(out CardData subCard))
            {
                CardTooltipUI cardTooltipUI = null;
                if (subCardTooltipUIs.Count <= subCardIndex)
                {
                    cardTooltipUI = Instantiate(cardTooltipUIPrefab, keywordUIParent);
                    subCardTooltipUIs.Add(cardTooltipUI);
                }
                else
                {
                    cardTooltipUI = subCardTooltipUIs[subCardIndex];
                }
                cardTooltipUI.ShowCard(new Card(subCard));
                subCardIndex++;
            }
        }

        for (; tooltipIndex < keywordTooltipUIs.Count; tooltipIndex++)
        {
            keywordTooltipUIs[tooltipIndex].Clear();
        }

        for (; subCardIndex < subCardTooltipUIs.Count; subCardIndex++)
        {
            subCardTooltipUIs[subCardIndex].ClearPreview();
        }

        xValueParent.gameObject.SetActive(false);
    }

    private void ShowCardTooltip(CardInspectedData data)
    {
        cardInspectedData = data;
        var card = data.card;
        isInFlight = data.InFlight;

        isShowingPreview = true;

        ShowCard(card);
    }

    private void RemoveCard(Card card)
    {
        int index = -1;
        for (int i = 0; i < inspectedCards.Count; i++)
        {
            if (inspectedCards[i].card == card)
            {
                index = i;
                break;
            }
        }

        if(index != -1)
        {
            inspectedCards.RemoveAt(index);
        }

        if (cardInspectedData.card == card)
        {
            if(inspectedCards.Count > 0)
            {
                ShowCardTooltip(inspectedCards[inspectedCards.Count - 1]);
            }
            else
            {
                ClearPreview();
            }
        }
    }

    private void ClearPreview()
    {
        isShowingPreview = false;
        cardTooltip.SetActive(false);
        foreach (var keyword in keywordTooltipUIs)
        {
            keyword.gameObject.SetActive(false);
        }

        foreach (var subCard in subCardTooltipUIs)
        {
            subCard.ClearPreview();
        }
    }

    private void Update()
    {
        if (!isShowingPreview)
        {
            return;
        }

        Vector2 anchoredPosition = canvasRectTransform.InverseTransformPoint(Input.mousePosition);
        float xMult = cardInspectedData.tooltipPivot.x - (1 - cardInspectedData.tooltipPivot.x);
        float yMult = cardInspectedData.tooltipPivot.y - (1 - cardInspectedData.tooltipPivot.y);
        if (cardInspectedData.inspectedRect != null)
        {
            anchoredPosition = canvasRectTransform.InverseTransformPoint(cardInspectedData.inspectedRect.position);
            anchoredPosition.x += xMult * (cardInspectedData.inspectedRect.rect.width * cardInspectedData.inspectedRect.localScale.x) / 2;
            anchoredPosition.y += yMult * (cardInspectedData.inspectedRect.rect.height * cardInspectedData.inspectedRect.localScale.y) / 2;
        }

        anchoredPosition.x += xMult * backgroundRectTransform.rect.width/2;
        anchoredPosition.y += yMult * backgroundRectTransform.rect.height/2;

        if (anchoredPosition.x + backgroundRectTransform.rect.width/2 + keywordUIParent.rect.width > canvasRectTransform.rect.width/2)
        {
            anchoredPosition.x = canvasRectTransform.rect.width/2 - backgroundRectTransform.rect.width/2 - keywordUIParent.rect.width;
        }

        if(anchoredPosition.x - backgroundRectTransform.rect.width/2 < - canvasRectTransform.rect.width/2)
        {
            anchoredPosition.x = -canvasRectTransform.rect.width/2 + backgroundRectTransform.rect.width/2;
        }

        if (anchoredPosition.y + backgroundRectTransform.rect.height/2 > canvasRectTransform.rect.height/2)
        {
            anchoredPosition.y = canvasRectTransform.rect.height/2 - backgroundRectTransform.rect.height/2;
        }

        if (anchoredPosition.y - backgroundRectTransform.rect.height/2 < -canvasRectTransform.rect.height/2)
        {
            anchoredPosition.y = -canvasRectTransform.rect.height/2 + backgroundRectTransform.rect.height/2;
        }           
        rectTransform.anchoredPosition = anchoredPosition;
        SetXValue(isInFlight ? CardXPreviewType.Flying : CardXPreviewType.Initial);
    }

    public void SetXValue(CardXPreviewType previewType, CharacterView targetView = null)
    {
        SetXValueInternal(previewType, targetView);
    }

    private void SetXValueInternal(CardXPreviewType previewType, CharacterView targetView)
    {
        var xData = cardInspectedData.card.XData;

        if (previewType == CardXPreviewType.TrySetTarget && targetView != null)
        {
            xData = cardInspectedData.card.CalculateXData(previewType, targetView.Character);
        }

        if (xData.IsVisible)
        {
            var colorForEffect = GlobalGameSettings.Settings.GetEffectColors(xData.EffectType);
            xValueParent.color = colorForEffect.BackgroundColor;
            xValue.color = colorForEffect.TextColor;
            xValue.text = xData.Value.ToString();
        }
        xValueParent.gameObject.SetActive(xData.IsVisible);
    }

    private void OnDestroy()
    {
        if (isMainCardTootltip)
        {
            EventBus.StopListening<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, SetCard);
            EventBus.StopListening<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, RemoveCard);
        }
    }
}


