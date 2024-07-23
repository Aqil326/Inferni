using UnityEngine;
using UnityEngine.UI;

public class CharmSlotUI : TargetableView
{
    [SerializeField]
    private Image selectedHighlight;

    [SerializeField]
    private SoundManager.Sound selectedSound;

    [SerializeField]
    private Image charmIcon;

    [SerializeField]
    private Sprite emptyCharmSlotSprite;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Color selectedOutlineColor;

    private CharmSlot charmSlot;
    private CharmData currentCharmData;
    private Color initialOutlineColor;

    protected override void Start()
    {
        base.Start();
        selectedHighlight.enabled = false;
        initialOutlineColor = selectedHighlight.color;
    }

    public void Init(CharmSlot charmSlot)
    {
        this.charmSlot = charmSlot;
        SetTargetable(charmSlot);
        OnCharmSlotChanged(charmSlot);

        charmSlot.CharmAddedEvent += OnCharmSlotChanged;
        charmSlot.CharmRemovedEvent += OnCharmSlotChanged;
    }

    private void OnCharmSlotChanged(CharmSlot charm)
    {
        if (charmSlot.IsEmpty)
        {
            ShowEmpty();
            return;
        }
        SetCharmIcon(charm.Charm.CharmData);
    }

    public void ShowEmpty()
    {
        currentCharmData = null;
        charmIcon.sprite = emptyCharmSlotSprite;
    }

    public void SetCharmIcon(CharmData charmData)
    {
        currentCharmData = charmData;
        charmIcon.sprite = charmData.charmIcon;
    }

    public override void TargetSelected(bool isValidTarget)
    {
        if (!isValidTarget) return;

        selectedHighlight.color = selectedOutlineColor;
        selectedSound.Play(false);
    }

    public override void TargetDeselected()
    {
        selectedHighlight.color = initialOutlineColor;
    }

    public void OnPointerEnter()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
        if (currentCharmData != null)
        {
            EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, new TooltipData()
            {
                Title = currentCharmData.charmName,
                Description = currentCharmData.description,
                IconImage = currentCharmData.charmIcon,
            });
        }
    }

    public void OnPointerExit()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
        EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);
    }

    protected override void OnCardDragStarted(Card card)
    {
        if(charmSlot == null)
        {
            return;
        }

        bool isTarget = (card.Data.Target == CardTarget.Charm && !charmSlot.IsEmpty) ||
            (card.Data.Target == CardTarget.EmptyCharmSlot && charmSlot.IsEmpty) ||
            card.Data.Target == CardTarget.Any;
        canvasGroup.blocksRaycasts = isTarget;
        selectedHighlight.enabled = isTarget;
    }

    protected override void OnCardDragEnded(Card card)
    {
        selectedHighlight.enabled = false;
        canvasGroup.blocksRaycasts = true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(charmSlot != null)
        {
            charmSlot.CharmAddedEvent -= OnCharmSlotChanged;
            charmSlot.CharmRemovedEvent -= OnCharmSlotChanged;
        }
    }
}
