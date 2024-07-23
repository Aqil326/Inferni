using UnityEngine;
using UnityEngine.UI;

public class SendCardToAllyUI : TargetableView
{
    [SerializeField]
    private Image outlineIcon;

    [SerializeField]
    private Color selectedColor;

    [SerializeField]
    private string sendToAllyTitle;

    [SerializeField]
    private string sendToAllyDescription;

    private Character character;
    private Color startingColor;

    protected override void Start()
    {
        base.Start();
        outlineIcon.gameObject.SetActive(false);
        startingColor = outlineIcon.color;
    }

    public override bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        canBeTargeted = true;
        return true;
    }

    public override void TargetDeselected()
    {
        outlineIcon.color = startingColor;
    }

    public override void TargetSelected(bool isValidTarget)
    {
        outlineIcon.color = selectedColor;
    }

    public void OnPointerEnter()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
        ShowSendAllyTooltip();
    }

    public void OnPointerExit()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
        ClearSendAllyTooltip();
    }

    protected override void OnCardDragStarted(Card card)
    {
        outlineIcon.gameObject.SetActive(true);
    }

    protected override void OnCardDragEnded(Card card)
    {
        outlineIcon.gameObject.SetActive(false);
    }

    public void ShowSendAllyTooltip()
    {
        EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, new TooltipData()
        {
            Title = sendToAllyTitle,
            Description = sendToAllyDescription
        });
    }

    public void ClearSendAllyTooltip()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);
    }
}
