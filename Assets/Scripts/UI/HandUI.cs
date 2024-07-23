using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class HandUI : MonoBehaviour
{
    [SerializeField]
    private CardUI cardUIPrefab;

    [SerializeField]
    private Transform cardUIParent;

    [SerializeField]
    private Transform deckUI;

    [SerializeField]
    private Transform discardPileUI;

    [SerializeField]
    private float cardDrawAnimationDuration;

    [SerializeField]
    private Vector3 draggingCardMouseOffset = new Vector3(0, 100, 0);

    [SerializeField]
    private float stopDragAnimationDuration = 0.5f;

    [SerializeField]
    private float dragCardScale = 0.5f;

    [SerializeField]
    private float initialCardDrawDelay = 0.5f;

    [SerializeField]
    private EventSystem eventSystem;

    [SerializeField]
    private Card3D card3DPrefab;

    [SerializeField]
    private RectTransform handOverlay;

    [SerializeField]
    private RectTransform handBackground;

    [SerializeField]
    private float cardBackgroundCardSize = 52;

    [SerializeField]
    private float cardBackgroundWidthOffset = 30;

    private RectTransform cardParentCanvas = null;
    
    private CardUI[] cards;
    private Character character;

    private Queue<CardUI> animationCardUIs = new Queue<CardUI>();
    private Card currentSelectedCard;
    private CardUI draggingCardUI;
    private CardUI draggingCardOrigin;
    private TargetableView currentDraggingTarget;
    private PointerEventData pointerEventData;
    private Card3D card3DInstance;
    private int currentMaxHand;

    struct CardAnimation
    {
        public CardUI card;
        public Sequence animation;
    }
    private Dictionary<int, CardAnimation> cardToHandAnimations = new Dictionary<int, CardAnimation>();

    public void Init(Character character)
    {
        this.character = character;
        
        cards = new CardUI[GlobalGameSettings.Settings.MaxHandSize];

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = Instantiate(cardUIPrefab, cardUIParent);
            cards[i].ClearCard();
            cards[i].InitHandCard(i, character);
            cards[i].gameObject.SetActive(i < character.HandSize);
            cards[i].name = "Card Slot " + i;
            cards[i].StartCardDragEvent += OnCardStartDrag;
            cards[i].RightClickedCardEvent += OnCardRightClick;
            if (i < character.Hand.Length)
            {
                StartCoroutine(StartCardDrawAnimation(character.Hand[i], i));
            }
        }

        currentMaxHand = character.Hand.Length;

        character.HandChangedEvent += OnHandChanged;
        character.CardDrawnEvent += OnCardDrawn;
        character.CardDrawnWithPositionEvent += OnCardDrawn;
        character.CardAddToDeckWithPositionEvent += OnCardAddToDeck;
        character.CardDiscardedEvent += OnCardDiscarded;
        character.CardDestroyedEvent += OnCardDestroyed;

        card3DInstance = Instantiate(card3DPrefab);
        card3DInstance.gameObject.SetActive(false);

        EventBus.StartListening<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, TrySetTargetable);
        EventBus.StartListening<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, ClearDraggingTarget);

        cardParentCanvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        handOverlay.SetAsLastSibling();
        OnHandChanged();
    }

    private void OnHandChanged()
    {
        currentMaxHand = character.Hand.Length;

        for(int i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(i < currentMaxHand);
        }
        //handBackground.sizeDelta = new Vector2(currentMaxHand * cardBackgroundCardSize + cardBackgroundWidthOffset, handBackground.sizeDelta.y);
        //handOverlay.sizeDelta = new Vector2(currentMaxHand * cardBackgroundCardSize + cardBackgroundWidthOffset, handBackground.sizeDelta.y);
    }

    private void OnCardDrawn(Card card, int index)
    {
        PlayAnimationAndSetCardInHand(card, index, deckUI.transform, Vector3.zero);
    }
    
    private void OnCardDrawn(Card card, int index, Vector3 worldPosition)
    {
        PlayAnimationAndSetCardInHand(card, index, cardParentCanvas, GetGlobalToUI(worldPosition, cardParentCanvas), false);       
    }
    
    private void OnCardAddToDeck(Card card, Vector3 worldPosition)
    {
        var cardAnimation = GetCardAnimation(); 
        cardAnimation.SetCard(card);
        
        var posInParent = GetGlobalToUI(worldPosition, cardParentCanvas);
        cardAnimation.transform.SetParent(cardParentCanvas);
        cardAnimation.transform.localPosition = posInParent;
        
        cardAnimation.transform.SetParent(deckUI);
        
        DOTween.Sequence().AppendCallback(() => cardAnimation.PlayFlipAnimation(null))
            .Join(cardAnimation.transform.DOLocalMove(Vector3.zero, cardDrawAnimationDuration))
            .AppendCallback(() => { EnqueueAnimationCard(cardAnimation); });
    }

    private void PlayAnimationAndSetCardInHand(Card card, int index, Transform parentTransform, Vector3 posInParent, bool showBack = true)
    {        
        var cardAnimation = GetCardAnimation();
        
        cardAnimation.SetCard(card, false, showBack);
        cardAnimation.transform.SetParent(parentTransform);
        cardAnimation.transform.localPosition = posInParent;

        CardUI cardUI = cards[index];

        cardAnimation.transform.SetParent(cardUI.transform);

        var sequence = DOTween.Sequence().Append(cardAnimation.transform.DOLocalMove(cardUI.VisualsParent.localPosition, cardDrawAnimationDuration))
            .Join(cardAnimation.transform.DOLocalRotate(cardUI.VisualsParent.localEulerAngles, cardDrawAnimationDuration))
            .AppendCallback(() =>
            {
                if (showBack)
                {
                    cardAnimation.PlayFlipAnimation(() => { SetCardInHand(cardAnimation, card, index); });
                }
                else
                {
                    SetCardInHand(cardAnimation, card, index);
                }
            });

        cardToHandAnimations.Add(index, new CardAnimation()
        {
            card = cardAnimation,
            animation = sequence,
        });

    }
    
    // Move the world position to UI
    private Vector3 GetGlobalToUI(Vector3 worldPosition, RectTransform rtCanvas)
    {
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rtCanvas, Camera.main.WorldToScreenPoint(worldPosition), null, out canvasPosition);
        return canvasPosition;
    }

    private Card GetSelectedCard() => currentSelectedCard;

    private IEnumerator StartCardDrawAnimation(Card card, int index)
    {
        yield return new WaitForSeconds(initialCardDrawDelay * (index + 1));
        OnCardDrawn(card, index);
    }

    private CardUI GetCardAnimation()
    {
        CardUI cardAnimation = null;
        if (animationCardUIs.Count > 0)
        {
            cardAnimation = animationCardUIs.Dequeue();
            cardAnimation.gameObject.SetActive(true);
            cardAnimation.transform.localScale = Vector3.one;
        }
        else
        {
            cardAnimation = Instantiate(cardUIPrefab, transform.parent);
            var canvas = cardAnimation.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1;
            cardAnimation.BlockPointerEvents();

        }
        return cardAnimation;
    }

    private void SetCardInHand(CardUI cardAnimation, Card card, int index)
    {
        if (cardAnimation)
        {
            EnqueueAnimationCard(cardAnimation);
            if (cardToHandAnimations.ContainsKey(index))
            {
                cardToHandAnimations.Remove(index);
            }
        }
        cards[index].SetHandCard(card);

;    }

    private void OnCardDiscarded(Card card, int index, float slowdown)
    {
        CardUI cardAnimation = null;
        cards[index].ClearCard();

        if (cardToHandAnimations.TryGetValue(index, out CardAnimation value))
        {
            cardAnimation = value.card;
            value.animation.Kill();
        }
        else if (draggingCardUI != null && draggingCardUI.Card == card)
        {
            cardAnimation = draggingCardUI;
            
            draggingCardUI.CanvasGroup.alpha = 1;
            OnCardStopDrag(false);
        }
        else
        {
            cardAnimation = GetCardAnimation();
            cardAnimation.SetCard(card);
            cardAnimation.transform.SetParent(cards[index].transform);
            cardAnimation.transform.localPosition = Vector3.zero;
        }
        cardAnimation.transform.SetParent(discardPileUI.transform);
        cardAnimation.OnCardDiscarded();
        DOTween.Sequence().AppendCallback(() => cardAnimation.PlayFlipAnimation(null))
                .Join(cardAnimation.transform.DOLocalMove(Vector3.zero, cardDrawAnimationDuration * slowdown))
                .AppendCallback(() => { EnqueueAnimationCard(cardAnimation); });
    }

    private void OnCardDestroyed(int index)
    {
        var card = character.Hand[index];
        CardUI cardAnimation = null;
        cards[index].ClearCard();
        if (cardToHandAnimations.TryGetValue(index, out CardAnimation value))
        {
            cardAnimation = value.card;
            value.animation.Kill();
        }
        else if (draggingCardUI != null && draggingCardUI.Card == card)
        {
            cardAnimation = draggingCardUI;

            draggingCardUI.CanvasGroup.alpha = 1;
            OnCardStopDrag(false);
        }
        else
        {
            cardAnimation = GetCardAnimation();
            cardAnimation.SetCard(card);
            cardAnimation.transform.SetParent(cards[index].transform);
            cardAnimation.transform.localPosition = Vector3.zero;
        }
        cardAnimation.PlayFlipAnimation(null);
        DOTween.Sequence().Append(cardAnimation.CanvasGroup.DOFade(0, cardDrawAnimationDuration))
               .AppendCallback(() => { EnqueueAnimationCard(cardAnimation); });

    }

    private void UpdateHandCardsPositions()
    {
        int siblingIndex = 0;
        foreach(var c in cards)
        {
            if(c.gameObject.activeSelf)
            {
                c.UpdateHandPosition(siblingIndex);
                siblingIndex++;
            }
        }
    }

    private void EnqueueAnimationCard(CardUI cardAnimation)
    {
        cardAnimation.gameObject.SetActive(false);
        cardAnimation.CanvasGroup.alpha = 1;
        animationCardUIs.Enqueue(cardAnimation);
    }

    private void OnCardStartDrag(CardUI card)
    {
        if(card.Card == null)
        {
            return;
        }

        draggingCardOrigin = card;
        draggingCardUI = GetCardAnimation();
        draggingCardUI.SetCard(card.Card);
        draggingCardUI.transform.localScale = Vector3.one * dragCardScale;
        currentSelectedCard = card.Card;
        card.ClearCard();
        draggingCardUI.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        draggingCardUI.StartDrag();
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, draggingCardUI.Card);
    }

    private void OnCardRightClick(CardUI card)
    {
        if (card.Card == null)
        {
            return;
        }

        character.SendCardToAlly(card.Card.HandIndex);
    }

    private void Update()
    {
        if(draggingCardUI != null && currentSelectedCard != null)
        {
            draggingCardUI.transform.position = Input.mousePosition + (draggingCardMouseOffset * Screen.height / 1080); ;

            if (Input.GetMouseButtonUp(0))
            {
                card3DInstance.gameObject.SetActive(false);
                draggingCardUI.CanvasGroup.alpha = 0;

                if (currentDraggingTarget != null)
                {
                    if (!character.TryPlayCard(draggingCardUI.Card.HandIndex, currentDraggingTarget))
                    { 
                        OnCardStopDrag(true);
                    }
                    else
                    {
                        currentSelectedCard = null;
                        ClearDraggingTarget();
                    }
                }
                else
                {
                    OnCardStopDrag(true);
                }
            }
        }
    }
   

    private void TrySetTargetable(TargetableView targetableView)
    {
        if(draggingCardUI == null)
        {
            return;
        }

        if(currentSelectedCard == null)
        {
            return;
        }

        if (targetableView == currentDraggingTarget)
        {
            return;
        }

        currentDraggingTarget = targetableView;
        bool isValidTarget = character.CanBeTarget(targetableView, currentSelectedCard);
        currentDraggingTarget.TargetSelected(isValidTarget);

        if (isValidTarget)
        {
            //Removing 3D card
            /*if (targetableView is CharacterView view)
            {
                card3DInstance.SetCard(draggingCardUI.Card, draggingCardUI.HandIndex);
                view.AddCard3D(card3DInstance);
                card3DInstance.SetCardTarget(view);
                card3DInstance.gameObject.SetActive(true);
                draggingCardUI.CanvasGroup.alpha = 0;
            }
            else*/
            {
                card3DInstance.gameObject.SetActive(false);
                draggingCardUI.CanvasGroup.alpha = 1;
            }
        }
    }


    private void ClearDraggingTarget(TargetableView target)
    {
        if (currentDraggingTarget == target)
        {
            ClearDraggingTarget();
        }
    }

    private void ClearDraggingTarget()
    {
        if(currentDraggingTarget == null)
        {
            return;
        }

        currentDraggingTarget.TargetDeselected();
        currentDraggingTarget = null;
        card3DInstance.gameObject.SetActive(false);

        if(draggingCardUI != null)
        {
            draggingCardUI.CanvasGroup.alpha = 1;
        }
    }

    private void OnCardStopDrag(bool playBacktoHandAnimation)
    {
        draggingCardUI.CanvasGroup.alpha = 1;
        ClearDraggingTarget();
        currentSelectedCard = null;
  
        draggingCardUI.StopDrag();
        draggingCardUI.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        if (playBacktoHandAnimation)
        {
            draggingCardUI.transform.SetParent(draggingCardOrigin.transform);
            DOTween.Sequence().Append(draggingCardUI.transform.DOLocalMove(Vector3.zero, stopDragAnimationDuration))
                .AppendCallback(() =>
                {
                    draggingCardOrigin.SetHandCard(draggingCardUI.Card);
                    draggingCardUI.transform.localScale = Vector3.one;
                    EnqueueAnimationCard(draggingCardUI);
                    ClearDraggingCard();
                });
        }
        else
        {
            ClearDraggingCard();
        }
    }

    private void ClearDraggingCard()
    {
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, draggingCardUI.Card);
        draggingCardUI = null;
        draggingCardOrigin = null;
    }

    private void OnDestroy()
    {
        if(character != null)
        {
            character.CardDrawnEvent -= OnCardDrawn;
            character.CardDrawnWithPositionEvent -= OnCardDrawn;
            character.CardAddToDeckWithPositionEvent -= OnCardAddToDeck;
            character.CardDiscardedEvent -= OnCardDiscarded;
            character.CardDestroyedEvent -= OnCardDestroyed;
            character.HandChangedEvent -= OnHandChanged;
        }

        EventBus.StopListening<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, TrySetTargetable);
        EventBus.StopListening<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, ClearDraggingTarget);
    }
}
