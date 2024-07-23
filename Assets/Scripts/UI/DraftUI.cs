using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class DraftUI : MonoBehaviour
{
    [SerializeField]
    private DraftEntryUI draftEntryUIPrefab;
    [SerializeField]
    private Canvas pickerCanvas;
    [SerializeField]
    private RectTransform canvasRectTransform;
    [SerializeField]
    private float selectedCardSizeMultiplier = 1.5f;
    [SerializeField]
    private DragOverButton deckCardTarget;
    [SerializeField]
    private DragOverButton[] charmTargets;
    [SerializeField]
    private DragOverButton handCardTarget;
    [SerializeField]
    private TextMeshProUGUI handCardTargetText;
    [SerializeField]
    private SoundManager.Sound pickCardSound;
    [SerializeField]
    private SoundManager.Sound pickCharmSound;

    [SerializeField]
    private DraftEntryUI draggingEntry;
    [SerializeField,  Range(0,1)]
    private float draggingEntryAlpha;

    [SerializeField]
    private RectTransform animatedDraftPackIcon;
    [SerializeField]
    private RectTransform draftPackParent;
    [SerializeField]
    private RectTransform draftPackImage;
    [SerializeField]
    private float draftPackInitialScale = 0.3f;
    [SerializeField]
    private float draftPackAnimationDuration = 1.5f;
    [SerializeField]
    private float cardsAnimationDuration = 0.8f;
    [SerializeField]
    private float delayBetweenCards = 0.1f;
    [SerializeField]
    private Transform draftPackRightPos;
    [SerializeField]
    private Transform draftPackLeftPos;
    [SerializeField]
    private float draftPackPassAnimationDuration;

    public int CardPickTimer { get; private set; }
    public bool HasPack => currentPack.HasValue;

    private DraftEntryUI currentSelectedEntry;
    private DraftPackData? currentPack;
    private Queue<DraftPackData> packQueue = new Queue<DraftPackData>();
    private CanvasGroup canvasGroup;
    private Character player;
    private List<DraftEntryUI> draftEntries = new List<DraftEntryUI>();
    private Queue<DraftEntryUI> draftEntryAnimations = new Queue<DraftEntryUI>();
    private RoundManager roundManager;
    private bool isHidden;
    private Button toggleButton;
    private bool isDragging;
    private RectTransform draggingEntryRectTransform;
    private DragOverButton currentTargetButton;
    private DG.Tweening.Sequence draftPackIconAnimation;
    private Vector3 initialPackImagePosition;
    private bool isPlayingPackPassAnimation;

    public Action DraftPickedEvent;

    private void Start()
    {
        roundManager = GameManager.GetManager<RoundManager>();
        draggingEntry.Hide();
        draggingEntryRectTransform = draggingEntry.GetComponent<RectTransform>();

        deckCardTarget.PointerEnterEvent += OnHoverButton;
        deckCardTarget.PointerExitEvent += OnHoverButtonExit;
        handCardTarget.PointerEnterEvent += OnHoverButton;
        handCardTarget.PointerExitEvent += OnHoverButtonExit;

        foreach (var target in charmTargets)
        {
            target.PointerEnterEvent += OnHoverButton;
            target.PointerExitEvent += OnHoverButtonExit;
        }

        animatedDraftPackIcon.gameObject.SetActive(false);
        initialPackImagePosition = draftPackImage.localPosition;
    }

    private void OnEnable()
    {
        EventBus.StartListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_COMPLETE_CLIENT, OnDraftPackCompleted);
        EventBus.StartListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_PASS_CLIENT, OnReceivedDraftPack);
        ClearDraggingTargets();
    }

    private void OnDisable()
    {
        EventBus.StopListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_COMPLETE_CLIENT, OnDraftPackCompleted);
        EventBus.StopListening<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_PASS_CLIENT, OnReceivedDraftPack);
    }


    public void Intialise(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var draftEntryUI = Instantiate(draftEntryUIPrefab, pickerCanvas.gameObject.transform);
            draftEntries.Add(draftEntryUI);
            draftEntryUI.Init(i);
            draftEntryUI.PickedEvent += OnItemPicked;
            draftEntryUI.StartedDragEvent += OnStartedDragging;

            var draftEntryUIAnimation = Instantiate(draftEntryUIPrefab, transform);
            draftEntryAnimations.Enqueue(draftEntryUIAnimation);
            draftEntryUIAnimation.Hide();
            draftEntryUIAnimation.transform.SetAsFirstSibling();
            var cg = draftEntryUIAnimation.GetComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        canvasGroup = GetComponent<CanvasGroup>();

        Hide();

        currentPack = null;

        player = GameManager.GetManager<CharacterManager>().PlayerCharacter;
    }

    private void OnReceivedDraftPack(DraftPackData draftPackData)
    {
        if (draftPackData.PreviousOwnerId == player.PlayerData.Id)
        {
            AfterDraftPick(draftPackData.LastPickedIndex);
            return;
        }

        //Ignore all non player characters
        if (draftPackData.OwnerId != player.PlayerData.Id)
        {
            return;
        }

        if (!currentPack.HasValue && !isPlayingPackPassAnimation)
        {
            currentPack = draftPackData;
            StartCoroutine(DraftPackReceivedCoroutine(draftPackData));
        }
        else
        {
            packQueue.Enqueue(draftPackData);
        }
    }

    private void OnDraftPackCompleted(DraftPackData draftPackData)
    {
        if (currentPack.HasValue && draftPackData.ID == currentPack.Value.ID)
        {
            currentPack = null;
            AfterDraftPick(draftPackData.LastPickedIndex);
        }
    }

    private IEnumerator DraftPackReceivedCoroutine(DraftPackData draftPackData)
    {
        yield return new WaitForSeconds(GlobalGameSettings.Settings.DraftPackTravelDuration);
        Vector2 canvasPosition = Camera.main.WorldToScreenPoint(player.CharacterView.boosterTarget.transform.position);
        animatedDraftPackIcon.localScale = Vector3.one * draftPackInitialScale;
        animatedDraftPackIcon.position = canvasPosition;
        animatedDraftPackIcon.gameObject.SetActive(true);
        draftPackIconAnimation = DOTween.Sequence().Append(animatedDraftPackIcon.transform.DOMove(draftPackImage.position, draftPackAnimationDuration))
                                                    .Join(animatedDraftPackIcon.DOScale(draftPackParent.localScale, draftPackAnimationDuration))
                                                    .AppendCallback(() =>
                                                    {
                                                        animatedDraftPackIcon.gameObject.SetActive(false);
                                                        Show();
                                                        draftPackIconAnimation = null;
                                 
                                                    });
    }

    private void PlayCardsAnimations()
    {
        for (int i = 0; i < draftEntries.Count; i++)
        {
            DraftEntryUI entryUI = draftEntries[i];
            CardData cardData = null;
            CharmData charmData = null;
            //Check if there is a card in the pack for this UI
            if (i < currentPack.Value.CardCount)
            {
                cardData = GameDatabase.GetCardData(currentPack.Value.CardList[i]); 
            }
            else if (i < currentPack.Value.CharmCount + currentPack.Value.CardCount)
            {
                charmData = GameDatabase.GetCharmData(currentPack.Value.CharmList[i - currentPack.Value.CardCount]);
            }
            else
            {
                entryUI.gameObject.SetActive(false);
                continue;
            }

            var animation = draftEntryAnimations.Dequeue();
            var rectTransform = animation.GetComponent<RectTransform>();
            rectTransform.position = draftPackImage.position;

            SetDraftEntryUI(animation, cardData, charmData);
            SetDraftEntryUI(entryUI, cardData, charmData);

            entryUI.Hide();
            animation.Show(isInteractable: false);
            StartCoroutine(WaitOneFrameCardAnimation(rectTransform, animation, entryUI, i));
        }
    }

    private IEnumerator WaitOneFrameCardAnimation(RectTransform rectTransform, DraftEntryUI animation, DraftEntryUI entryUI, int i)
    {
        //Waiting a frame so that entry UI layout is updated
        yield return null;
        StartCoroutine(CardAnimationCoroutine(rectTransform, animation, entryUI.transform.position, i, true, () => { entryUI.Show(); }));
    }

    private int cardBackAnimationsPlaying = 0;
    private void PlayCardBackAnimations()
    {
        for (int i = 0; i < draftEntries.Count; i++)
        {
            DraftEntryUI entryUI = draftEntries[i];
            if(entryUI.gameObject.activeSelf && !entryUI.IsHidden)
            {
                var animation = draftEntryAnimations.Dequeue();
                var rectTransform = animation.GetComponent<RectTransform>();
                rectTransform.position = entryUI.transform.position;

                CardData cardData = null;
                CharmData charmData = null;
                if(entryUI.Type == DraftEntryUI.EntryType.Card)
                {
                    cardData = entryUI.GetCard().Data;
                }
                else if (entryUI.Type == DraftEntryUI.EntryType.Charm)
                {
                    charmData = entryUI.GetCharm().CharmData;
                }
                SetDraftEntryUI(animation, cardData, charmData);

                entryUI.Hide();
                animation.Show(isInteractable: false);

                cardBackAnimationsPlaying++;
                StartCoroutine(CardAnimationCoroutine(rectTransform, animation, draftPackImage.transform.position, i, false, CardBackAnimationsFinished));
            }
        }
    }

    private void CardBackAnimationsFinished()
    {
        cardBackAnimationsPlaying--;
        if(cardBackAnimationsPlaying == 0)
        {
            PlayPackPassAnimation();
        }
    }

    private void PlayPackPassAnimation()
    {
        DOTween.Sequence().Append(draftPackImage.transform.DOMove(draftPackLeftPos.position, draftPackPassAnimationDuration))
            .AppendCallback(()=>
            {
                isPlayingPackPassAnimation = false;
                if (packQueue.Count > 0)
                {
                    currentPack = packQueue.Dequeue();
                    DequeuePackAnimation();
                }
                else
                {
                    draftPackImage.transform.localPosition = Vector3.zero;
                    Hide();
                }
            });
    }

    private IEnumerator CardAnimationCoroutine(RectTransform rectTransform, DraftEntryUI animation, Vector3 target, int index, bool startAnimationWithCardBack, Action animationEndCallback)
    {
        yield return new WaitForSeconds(index * delayBetweenCards);
        if(animation.Type == DraftEntryUI.EntryType.Card)
        {
            animation.PlayCardFlipAnimation(cardsAnimationDuration, startAnimationWithCardBack);
        }
        DOTween.Sequence().Append(rectTransform.DOMove(target, cardsAnimationDuration))
                                .AppendCallback(() =>
                                {
                                    animationEndCallback?.Invoke();
                                    animation.Hide();
                                    draftEntryAnimations.Enqueue(animation);
                                });
    }

    private void SetDraftEntryUI(DraftEntryUI entryUI, CardData cardData, CharmData charmData)
    {
        entryUI.gameObject.SetActive(true);
        if(cardData != null)
        {
            Card card = new Card(cardData);
            entryUI.SetCard(card);
        }
        else if(charmData != null)
        {
            Charm charm = new Charm(charmData);
            entryUI.SetCharm(charm);
        }
    }

    public void SetToggleButton(Button button)
    {
        if (!button) return;

        toggleButton = button;
        toggleButton.onClick.AddListener(OnToggleButtonClicked);
        SetToggleButtonVisibility(false);
    }

    public void SetToggleButtonVisibility(bool isVisible)
    {
        if (toggleButton)
        {
            toggleButton.gameObject.SetActive(isVisible);
        }
    }

    private void OnToggleButtonClicked()
    {
        isHidden = !isHidden;
        SetDraftUIVisibility();
    }

    private void SetDraftUIVisibility()
    {
        if (isHidden)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (currentPack is null)
            {
                SetToggleButtonVisibility(false);
            }
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            SetToggleButtonVisibility(true);
        }
    }

    private void Show()
    {
        isHidden = false;

        PlayCardsAnimations();
        SetDraftUIVisibility();
    }

    private void Hide()
    {
        isHidden = true;
        SetDraftUIVisibility();
    }

    private void OnItemPicked(DraftEntryUI entry)
    {
        if(currentPack == null)
        {
            Debug.LogError("Shouldn't be able to pick a Draft Item if the current pack is null!");
            return;
        }

        if (entry.Type == DraftEntryUI.EntryType.Card)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, Input.mousePosition, Camera.main, out Vector3 worldPoint);
            if (player.HandSize < player.Hand.Length)
            {               
                AddCardToHand(entry, worldPoint);
            }
            else
            {
                PickCardToDeck(entry, worldPoint);
            }
        }
        else if (entry.Type == DraftEntryUI.EntryType.Charm)
        {
            int slotIndex = player.CharmSlots.Length - 1;

            for (int i = 0; i < slotIndex; i++)
            {
                if (player.CharmSlots[i].IsEmpty)
                {
                    slotIndex = i;
                    break;
                }
            }
            AddCharmToSlot(entry, slotIndex);
        }
    }

    public void PickAndAddCardToDeck()
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, Input.mousePosition, Camera.main, out Vector3 worldPoint);
        PickCardToDeck(currentSelectedEntry, worldPoint);
    }

    private void PickCardToDeck(DraftEntryUI draftEntry, Vector2 position)
    {
        if (currentPack == null)
        {
            Debug.LogError("Shouldn't be able to pick a Draft Item if the current pack is null!");
            return;
        }
        draftEntry.Hide();
        player.AddDraftCardToDeckServerRPC(draftEntry.GetCard().Data.InternalID, currentPack.Value.ID, position);
        pickCardSound.Play(false);
    }

    public void PickAndAddCardToHand()
    {
        if (player.HandSize < player.Hand.Length)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, Input.mousePosition, Camera.main, out Vector3 worldPoint);
            AddCardToHand(currentSelectedEntry, worldPoint);
        }
        else
        {
            PickAndAddCardToDeck();
        }
    }

    private void AddCardToHand(DraftEntryUI draftEntry, Vector2 position)
    {
        if (currentPack == null)
        {
            Debug.LogError("Shouldn't be able to pick a Draft Item if the current pack is null!");
            return;
        }
        draftEntry.Hide();
        player.AddDraftCardToHandServerRPC(draftEntry.GetCard().Data.InternalID, currentPack.Value.ID, position);
        pickCardSound.Play(false);
    }

    public void AddCharmToSlot(int slot)
    {
        AddCharmToSlot(currentSelectedEntry, slot);
    }

    private void AddCharmToSlot(DraftEntryUI draftEntry, int slot)
    {
        draftEntry.Hide();
        player.AddDraftCharmToSlotServerRPC(draftEntry.GetCharm().CharmData.InternalID, slot, currentPack.Value.ID);
        pickCharmSound.Play(false);
    }

    public void OnCharmIconInspected(int index)
    {
        var charmSlot = player.CharmSlots[index];

        if (charmSlot.IsEmpty)
        {
            return;
        }

        var charmData = charmSlot.Charm.CharmData;
        EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, new TooltipData()
        {
            Title = charmData.charmName,
            Description = charmData.description,
            IconImage = charmData.charmIcon,
        });
    }

    public void ClearCharmIconInspected()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);
    }

    private void AfterDraftPick(int lastPickedIndex)
    {
        for (int i = 0; i < draftEntries.Count; i++)
        {
            DraftEntryUI entry = draftEntries[i];
            if(i == lastPickedIndex)
            {
                //hide picked entry
                entry.Hide();
            }
            entry.ClearHover();
        }

        if (currentPack != null)
        {
            if(isDragging)
            {
                StopDragging();
            }
            currentPack = null;
            isPlayingPackPassAnimation = true;
            PlayCardBackAnimations();
        }
        else
        {
            Hide();
        }

        DraftPickedEvent?.Invoke();
    }

    private void DequeuePackAnimation()
    {
        draftPackImage.transform.position = draftPackRightPos.position;
        DOTween.Sequence().Append(draftPackImage.transform.DOLocalMove(initialPackImagePosition, draftPackPassAnimationDuration))
            .AppendCallback(() =>
            {
                if (currentPack.HasValue)
                {
                    Show();
                }
            });
    }

    private void OnStartedDragging(DraftEntryUI draftEntry)
    {
        if(isPlayingPackPassAnimation)
        {
            return;
        }

        currentSelectedEntry = draftEntry;
        draftEntry.Hide();
        if (draftEntry.Type == DraftEntryUI.EntryType.Card)
        {
            draggingEntry.SetCard(draftEntry.GetCard());
            
            deckCardTarget.gameObject.SetActive(true);
            handCardTarget.gameObject.SetActive(true);
            handCardTargetText.gameObject.SetActive(player.HandSize < player.Hand.Length);

        }
        else if (draftEntry.Type == DraftEntryUI.EntryType.Charm)
        {
            draggingEntry.SetCharm(draftEntry.GetCharm());
            foreach (var target in charmTargets)
            {
                target.gameObject.SetActive(true);
            }
        }
        draggingEntry.Show(draggingEntryAlpha, false);
        isDragging = true;
    }

    private void ClearDraggingTargets()
    {
        deckCardTarget.gameObject.SetActive(false);
        handCardTarget.gameObject.SetActive(false);
        
        foreach (var target in charmTargets)
        {
            target.gameObject.SetActive(false);
        }
    }

    private void OnHoverButton(DragOverButton button)
    {
        if(!isDragging)
        {
            return;
        }

        button.Select();
        currentTargetButton = button;
    }

    private void OnHoverButtonExit(DragOverButton button)
    {
        if (!isDragging)
        {
            return;
        }

        button.Deselect();
        currentTargetButton = null;
    }

    private void StopDragging()
    {
        draggingEntry.Hide();
        isDragging = false;
        currentSelectedEntry = null;
        if (currentTargetButton != null)
        {
            currentTargetButton.Deselect();
            currentTargetButton = null;
        }
        ClearDraggingTargets();
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector2 anchoredPosition = canvasRectTransform.InverseTransformPoint(Input.mousePosition);
            draggingEntryRectTransform.anchoredPosition = anchoredPosition;

            if (Input.GetMouseButtonUp(0))
            {
                if (currentTargetButton != null)
                {
                    currentTargetButton.Activate();
                }
                else
                {
                    currentSelectedEntry.Show();
                }
                StopDragging();
            }
        }
    }
}
