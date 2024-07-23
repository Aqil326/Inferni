using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CardUI : TargetableView, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler
{
    [SerializeField]
    private CastCostUI[] castCostUIs;

    [SerializeField]
    private TextMeshProUGUI cardName;
    
    [SerializeField] 
    private Image mainImage;

    [SerializeField]
    private TextMeshProUGUI castTimeText;

    [SerializeField]
    private Image cardOutline;

    [SerializeField]
    private Image coloredBackground;
    
    [SerializeField]
    private Image cardNumberParent;

    [SerializeField]
    private TextMeshProUGUI targetText;
    
    [SerializeField]
    private TextMeshProUGUI cardNumberText;

    [SerializeField]
    private Image targetIcon;

    [SerializeField]
    private GameObject cardNumberParentGO;

    [SerializeField]
    private CanvasGroup cardFrontCanvasGroup;

    [SerializeField]
    private CanvasGroup cardBackCanvasGroup;

    [SerializeField]
    private CanvasGroup cardTimerCanvasGroup;

    [SerializeField]
    private float flipAnimationDuration;

    [SerializeField]
    private float castableTransformOffset = 10f;

    [SerializeField]
    private SoundManager.Sound selectedSound;

    [SerializeField]
    private SoundManager.Sound flipSound;

    [SerializeField]
    private Color playableColor;

    [SerializeField]
    private Color unplayableColor;

    [SerializeField]
    private List<Image> colorBasedImages;

    [SerializeField]
    private List<TextMeshProUGUI> colorBasedTexts;

    [SerializeField]
    private CanvasGroup castOverlay;

    [SerializeField]
    private TextMeshProUGUI castTimer;

    [SerializeField]
    private Image castTimerImage;

    [SerializeField]
    private Color cardSelectedColor;

    [SerializeField]
    private Transform cardVisualsParent;

    [SerializeField]
    private CurveParameters handPositionCurveParameters;

    [SerializeField]
    private float hoverPunchAngle, hoverTransition;

    [SerializeField]
    private float scaleOnHover, scaleTransition;
    [SerializeField]
    private Ease scaleEase;

    public CanvasGroup CanvasGroup;

    public event Action<CardUI> StartCardDragEvent;
    public event Action<CardUI> RightClickedCardEvent;
    public event Action<CardUI> CardSelectedEvent;

    public Card Card { get; private set; }
    public int HandIndex { get; private set; }
    public Transform VisualsParent => cardVisualsParent;

    private GlobalGameSettings gameSettings;
    private bool isHandCard;
    private bool isClickable;
    private bool isFaceUp = true;
    private bool isDraft = false;
    private bool isDraftSelected = false;
    private bool is3D = false;
    private bool isDragging;
    private bool isPointerOver;
    private bool isPointerBlocked;
    private RectTransform rectTransform;
    private Color initialOutlineColor;

    private CardXPreviewType xPreviewType = CardXPreviewType.Initial;
    private CharacterView targetCharacterView;
    private float originalHeight;
    private CardInspectedData cardInspectedData;
    private float totalDisabledTime;
    private bool isCardCastable;
    private Character handOwner;
    private int siblingIndex = 0;

    [Serializable]
    public class CardColorSettings
    {
        public CardColors color;
        public Sprite sprite;
        public Color backgroundColor;
    }

    [SerializeField]
    private List<CardColorSettings> colorBackgrounds;


    private void Awake()
    {
        gameSettings = GlobalGameSettings.Settings;
        rectTransform = GetComponent<RectTransform>();
        originalHeight = rectTransform.sizeDelta.y;
        HideDisabledOverlay();

        initialOutlineColor = cardOutline.color;
    }

    public void SetCard(Card card, bool isClickable = false, bool showBack = false, bool is3D = false)
    {
        CanvasGroup.alpha = 1;

        this.isClickable = isClickable;
        isFaceUp = !showBack;
        this.is3D = is3D;

        Card = card;
        Card.SetCardUI(this);
        SetTargetable(Card);

        Card.SetCastCostUIs(castCostUIs);

        cardName.text = card.Data.Name;
        mainImage.sprite = card.Data.CardSprite;
        SetCardCastingTimeText();

        bool bgFound = false;
        Color color = Color.white;
        foreach (var bgs in colorBackgrounds)
        {
            if (bgs.color == card.Data.CardColor)
            {
                bgFound = true;
                coloredBackground.sprite = bgs.sprite;
                color = bgs.backgroundColor;
                break;
            }
        }

        if(!bgFound)
        {
            Debug.LogError($"Couldn't find Colored Background for color {card.Data.CardColor}");
        }

        foreach(var image in colorBasedImages)
        {
            image.color = color;
        }

        foreach (var text in colorBasedTexts)
        {
            text.color = color;
        }

        var icon = GlobalGameSettings.Settings.GetTargetIcon(Card.Data.Target);
        if(targetIcon != null)
        {
            targetIcon.sprite = icon.sprite;
            targetText.text = icon.targetText;
        }

        coloredBackground.color = unplayableColor;
       
        SetRightSideCardGraphics();
        
        if (card.Owner != null)
        {
            card.Owner.CastingTimeChange.OnValueChanged += OnOwnerCastingTimeChanged;
        }
    }

    private void SetCardCastingTimeText()
    {
        var castingTime = Card.GetCardCastingTime();
        castTimeText.text = castingTime + "s";
        castTimeText.transform.parent.gameObject.SetActive(castingTime != 0);
    }
    
    private void OnOwnerCastingTimeChanged(float oldCastingTimeChange, float newCastingTimeChange)
    {
        if (Card == null) return;
        SetCardCastingTimeText();
    }

    private bool IsDisabled(ref float currentDisabledTimer)
    {
        if (Card == null || Card.Owner == null) return false;

        if (!Card.Owner.CanPlayCards())
        {
            return true;
        }
 
        if (Card.Owner.Modifier is FreezeModifier modifier)
        {
            totalDisabledTime = Card.Owner.Modifier.Duration;
            var duration = modifier.Duration;
            currentDisabledTimer = duration - Card.Owner.RemainingModifierTime.Value;
        }

        if (Card.Owner.IsCasting.Value)
        {
            totalDisabledTime = Card.GetCardCastingTime();
            currentDisabledTimer = Card.Owner.RemainingCastingTime.Value;
        }
        return false;
    }

    public void InitHandCard(int handIndex, Character owner)
    {
        HandIndex = handIndex;
        isHandCard = true;
        handOwner = owner;
        owner.CardCastingStartEvent += (index, castingTime, castingType) => CheckCardPlayability();
        owner.CardCastingEndEvent += CheckCardPlayability;
        owner.RemainingCastingTime.OnValueChanged += (previous, current) => UpdateDisabledTimer(previous, current);
        owner.ModifierAddedEvent += OnModifierAdded;
        owner.ModifierRemovedEvent += OnModifierRemoved;
        owner.RemainingModifierTime.OnValueChanged += OnRemainingModifierTimeChanged;
        owner.CardPlayabilityChangedEvent += CheckCardPlayability;
        owner.StateChangedEvent += CheckCardPlayability;
    }

    public void SetHandCard(Card card,  bool isClickable = true, bool showBack = false, bool is3D = false)
    {
        if (card == this.Card)
        {
            return;
        }
        
        SetCard(card, isClickable, showBack, is3D);
        
        isCardCastable = Card.IsCastable();
        CheckCardPlayability();
        
        CanvasGroup.transform.localPosition = Vector3.zero;
    }

    
    public void UpdateHandPosition(int siblingIndex)
    {
        if(handOwner == null)
        {
            return;
        }

        float siblingPercentage = 0.5f;

        if (handOwner.HandSize > 1)
        {
            siblingPercentage = (float)siblingIndex / (float)(handOwner.HandSize - 1);
        }

        this.siblingIndex = siblingIndex;
        float curveYOffset = (handPositionCurveParameters.positioning.Evaluate(siblingPercentage) * handPositionCurveParameters.positioningInfluence);
        cardVisualsParent.localPosition = Vector3.up * curveYOffset;

        float tiltZ = handPositionCurveParameters.rotation.Evaluate(siblingPercentage) * (handPositionCurveParameters.rotationInfluence);
        cardVisualsParent.eulerAngles = new Vector3(cardVisualsParent.eulerAngles.x, cardVisualsParent.eulerAngles.y, tiltZ);
    }

    private void CheckCardPlayability(Character character)
    {
        CheckCardPlayability();
    }

    private void CheckCardPlayability()
    {
        float currentDisabledTimer = 0;
        if (IsDisabled(ref currentDisabledTimer) || !isCardCastable)
        {
            ShowDisabledOverlay();
            if (currentDisabledTimer > 0)
            {
                UpdateDisabledTimer(0, currentDisabledTimer, false);
            }
            else
            {
                HideDisabledTimer();
            }
        }
        else
        {
            HideDisabledOverlay();
        }
    }

    private void OnRemainingModifierTimeChanged(float previous, float current)
    {
        if (Card == null || Card.Owner == null || Card.Owner.Modifier is not FreezeModifier modifier)
        {
            return;
        }

        var duration = modifier.Duration;
        UpdateDisabledTimer(duration - previous, duration - current);
    }

    private void OnModifierRemoved(CharacterModifier modifier)
    {
        if (modifier is not FreezeModifier) return;
        totalDisabledTime = 0;
        CheckCardPlayability();
    }

    private void OnModifierAdded(CharacterModifier modifier)
    {
        if (modifier is not FreezeModifier) return;
        totalDisabledTime = modifier.Duration;
        CheckCardPlayability();
    }

    public void SetXValue(CardXPreviewType previewType, CharacterView targetView = null)
    {
        xPreviewType = previewType;
        targetCharacterView = targetView;
        SetXValueInternal();
    }

    private void SetXValueInternal()
    {        
        var xData = Card.XData;

        if(xPreviewType == CardXPreviewType.TrySetTarget && targetCharacterView != null)
        {
            xData = Card.CalculateXData(xPreviewType, targetCharacterView.Character);
        }

        if (xData.IsVisible)
        {
            var colorForEffect = GlobalGameSettings.Settings.GetEffectColors(xData.EffectType);
            cardNumberParent.color = colorForEffect.BackgroundColor;
            cardNumberText.color = colorForEffect.TextColor;
            cardNumberText.text = xData.Value.ToString();
        }
        cardNumberParentGO.SetActive(xData.IsVisible);
    }

    private void ShowDisabledOverlay()
    {
        castOverlay.alpha = 1;
    }

    private void UpdateDisabledTimer(float previous, float current, bool overrideTotalDisabledTime = true)
    {
        if (overrideTotalDisabledTime)
        {
            if (current > previous)
            {
                totalDisabledTime = current;
            }    
        }
        cardTimerCanvasGroup.alpha = 1;
        castTimerImage.fillAmount = current / totalDisabledTime;
        castTimer.text = string.Format("{0}s", (int)current);
    }

    private void HideDisabledTimer()
    {
        cardTimerCanvasGroup.alpha = 0;
    }

    private void HideDisabledOverlay(bool isModifierEnd = false)
    {
        var shouldHideOverlay = true;
        var isFrozen = Card != null && Card.Owner != null && Card.Owner.Modifier is FreezeModifier;
        
        if (!isModifierEnd && isFrozen)
        {
            shouldHideOverlay = false;
        }
        
        if (shouldHideOverlay)
        {
            castOverlay.alpha = 0;
        }
    }

    private void Update()
    {
        if (Card != null)
        {
            SetXValueInternal();

            if(Card.IsCastable() != isCardCastable)
            {
                isCardCastable = Card.IsCastable();
                CheckCardPlayability();
            }
        }
    }

    public void ClearCard()
    {
        TryClearCardInspected();

        Card = null;
        CanvasGroup.alpha = 0;
        isDragging = false;
    }

    public void OnBeginDrag()
    {
        var timer = 0f;
        if (!isClickable || IsDisabled(ref timer) || isPointerBlocked)
        {
            return;
        }
        ClearCardInspected();
        StartCardDragEvent?.Invoke(this);
    }

    public void StartDrag()
    {
        isDragging = true;
        SendCardInspected();
    }

    public void StopDrag()
    {
        isDragging = false;
        ClearCardInspected();
    }

    public void OnCardDiscarded()
    {
        TryClearCardInspected();
        
        if (Card.Owner != null)
        {
            Card.Owner.CastingTimeChange.OnValueChanged -= OnOwnerCastingTimeChanged;
        }
    }

    public void Select()
    {
        cardOutline.color = cardSelectedColor;
        selectedSound.Play(false);
        CardSelectedEvent?.Invoke(this);
    }

    public void Deselect()
    {
        cardOutline.color = initialOutlineColor;
    }

    public void PlayFlipAnimation(Action animationEndCallback)
    {
        PlayFlipAnimation(animationEndCallback, flipAnimationDuration);
    }

    public void PlayFlipAnimation(Action animationEndCallback, float duration)
    {
        DOTween.Sequence().Append(cardVisualsParent.DOLocalRotate(new Vector3(cardVisualsParent.localRotation.x, 90, cardVisualsParent.localRotation.z), duration / 2))
            .AppendCallback(() =>
            {
                isFaceUp = !isFaceUp;
                SetRightSideCardGraphics();
            })
            .Append(cardVisualsParent.DOLocalRotate(new Vector3(cardVisualsParent.localRotation.x, 0, cardVisualsParent.localRotation.z),  duration/ 2))
            .AppendCallback(()=> { animationEndCallback?.Invoke(); });

        flipSound.Play(false);
    }

    public void ShowFront()
    {
        isFaceUp = true;
        SetRightSideCardGraphics();
    }

    public void ShowBack()
    {
        isFaceUp = false;
        SetRightSideCardGraphics();
    }

    private void SetRightSideCardGraphics()
    {
        cardBackCanvasGroup.alpha = isFaceUp ? 0 : 1;
        cardFrontCanvasGroup.alpha = isFaceUp ? 1 : 0;
    }

    public override bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        if(Card == null)
        {
            canBeTargeted = false;
            return true;
        }

        return base.ShouldOverrideTargetAvailability(ref canBeTargeted, card);
    }

    public override void TargetSelected(bool isValidTarget)
    {
        if (isValidTarget)
        {
            Select();    
        }
    }

    public override void TargetDeselected()
    {
        Deselect();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        if (Card == null || isPointerBlocked)
        {
            return;
        }
        SendCardInspected();
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);

        if (isHandCard)
        {
            cardVisualsParent.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase).SetId(3);

            DOTween.Kill(2, true);
            cardFrontCanvasGroup.transform.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        if (Card == null || isPointerBlocked)
        {
            return;
        }


        ClearCardInspected();
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if(isPointerBlocked)
        {
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClickedCardEvent?.Invoke(this);
        }
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (isPointerBlocked)
        {
            return;
        }
        OnBeginDrag();
    }

    private void SendCardInspected()
    {
        EventBus.TriggerEvent<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, new CardInspectedData()
        {
            card = Card,
            InFlight = false,
            inspectedRect = is3D ? null : GetComponent<RectTransform>(),
            tooltipPivot = isDragging ? new Vector2(1, 0) : new Vector2(0.5f, 1)
        });
    }

    private void ClearCardInspected()
    {
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, Card);
        if (isHandCard)
        {
            cardVisualsParent.DOScale(1, scaleTransition).SetEase(scaleEase).SetId(3);
        }
    }

    public void TryClearCardInspected()
    {
        if(isPointerOver)
        {
            ClearCardInspected();
            isPointerOver = false;
        }
    }

    private void OnDisable()
    {
        TryClearCardInspected();
    }

    protected override void OnCardDragStarted(Card card)
    { 
        bool isTarget = card.Data.Target == CardTarget.CardInHand;
        CanvasGroup.blocksRaycasts = isTarget;
        cardOutline.gameObject.SetActive(isTarget);
    }

    protected override void OnCardDragEnded(Card card)
    {
        cardOutline.gameObject.SetActive(false);
        CanvasGroup.blocksRaycasts = true;
    }

    public void BlockPointerEvents()
    {
        isPointerBlocked = true;
    }
}
