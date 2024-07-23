using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CastingIcon : TargetableView, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Image castingSprite;
    
    [SerializeField] 
    private IconWithMaskController icon;
    
    [SerializeField]
    private Image timeImage;
    [SerializeField]
    private Sprite energyCastSprite;
    [SerializeField]
    private Sprite sendCardCastSprite;
    [SerializeField]
    private Image selectedHighlight;
    [SerializeField]
    private TextMeshProUGUI timeText;
    [SerializeField]
    private CanvasGroup canvasGroup;

    public Character Owner { get; private set; }

    private CharacterManager characterManager;
    private float totalCastingTime;
    private Card card;
    private bool isPointerOver;

    public void Init(Character owner)
    {
        if(Owner != null)
        {
            Owner.CardCastingStartEvent -= StartCasting;
            Owner.CardCastingEndEvent -= EndCasting;
            Owner.RemainingCastingTime.OnValueChanged -= UpdateTimer;
        }
        
        Owner = owner;
        characterManager = GameManager.GetManager<CharacterManager>();
        owner.CardCastingStartEvent += StartCasting;
        owner.CardCastingEndEvent += EndCasting;
        owner.RemainingCastingTime.OnValueChanged += UpdateTimer;

        selectedHighlight.enabled = false;
        gameObject.SetActive(false);

        SetTargetable(Owner.CastingTargetable);
    }

    public void CancelCasting()
    {
        if(!Owner.IsCasting.Value)
        {
            //Do nothing
            return;
        }
        Owner.CancelCasting();
        gameObject.SetActive(false);
    }

    private void StartCasting(int cardIndex, float castingTime, CastingType castingType)
    {
        var card = Owner.Hand[cardIndex];
        gameObject.SetActive(true);
        totalCastingTime = castingTime;
        if(castingType == CastingType.Energy)
        { 
            icon.InitIcon(energyCastSprite);
            castingSprite.color = GlobalGameSettings.Settings.GetCardColor(card.Data.CardColor);
        }
        else if(castingType == CastingType.CardSend)
        {
            icon.InitIcon(sendCardCastSprite);
        }
        else
        {
            this.card = card; 
            icon.InitIcon(card.Data.CardSprite);  
        }
    }

    private void UpdateTimer(float previous, float current)
    {
        if(current > previous)
        {
            totalCastingTime = current;
        }
        timeImage.fillAmount = current / totalCastingTime;
        timeText.text = string.Format("{0}s",(int)current);
    }

    private void EndCasting()
    {
        castingSprite.color = Color.white;
        gameObject.SetActive(false);
    }

    public void OnPointerEnter()
    {
        if(card == null)
        {
            return;
        }
        isPointerOver = true;
        var cardInspectedData = new CardInspectedData()
        {
            card = card,
            InFlight = false,
            tooltipPivot = new Vector3(0.5f, 1)
        };
        EventBus.TriggerEvent<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, cardInspectedData);
    }

    public void OnPointerExit()
    {
        if (card == null)
        {
            return;
        }
        isPointerOver = false;
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, card);
    }

    public override void TargetSelected(bool isValidTarget)
    {
        if (isValidTarget)
        {
            selectedHighlight.enabled = true;
        }
    }

    public override void TargetDeselected()
    {
        selectedHighlight.enabled = false;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    private void OnDisable()
    {
        if (isPointerOver)
        {
            OnPointerExit();
        }
    }

    protected override void OnCardDragStarted(Card card)
    {
        canvasGroup.blocksRaycasts = card.Data.Target == CardTarget.SpellInCast;
    }

    protected override void OnCardDragEnded(Card card)
    {
        canvasGroup.blocksRaycasts = true;
    }
}
