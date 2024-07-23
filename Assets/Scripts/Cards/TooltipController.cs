using UnityEngine;

public class TooltipController : MonoBehaviour
{
    [SerializeField]
    private TooltipUI tooltip;

    [SerializeField]
    private RectTransform canvasRectTransform;

    private bool isShowingTooltip;
    private RectTransform tooltipRectTransform;
    private TooltipData tooltipData;

    private void Awake()
    {
        EventBus.StartListening<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, ShowTooltip);
        EventBus.StartListening(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT, HideTooltip);
        tooltipRectTransform = tooltip.GetComponent<RectTransform>();
    }

    public void ShowTooltip(string description)
    {
        ShowTooltip(new TooltipData()
        {
            Description = description
        });
    }

    private void ShowTooltip(TooltipData tooltipData)
    {
        this.tooltipData = tooltipData;
        isShowingTooltip = true;
        tooltip.Show(tooltipData);
    }

    public void HideTooltip()
    {
        isShowingTooltip = false;
        tooltip.Clear();
    }

    private void Update()
    {
        if(!isShowingTooltip)
        {
            return;
        }

        Vector2 anchoredPosition = canvasRectTransform.InverseTransformPoint(Input.mousePosition);

        anchoredPosition.x -= tooltipRectTransform.rect.width / 2;

        if (anchoredPosition.x + tooltipRectTransform.rect.width / 2 > canvasRectTransform.rect.width / 2)
        {
            anchoredPosition.x = canvasRectTransform.rect.width / 2 - tooltipRectTransform.rect.width / 2;
        }

        if (anchoredPosition.x - tooltipRectTransform.rect.width / 2 < -canvasRectTransform.rect.width / 2)
        {
            anchoredPosition.x = -canvasRectTransform.rect.width / 2 + tooltipRectTransform.rect.width / 2;
        }

        if (anchoredPosition.y + tooltipRectTransform.rect.height / 2 > canvasRectTransform.rect.height / 2)
        {
            anchoredPosition.y = canvasRectTransform.rect.height / 2 - tooltipRectTransform.rect.height / 2;
        }

        if (anchoredPosition.y - tooltipRectTransform.rect.height / 2 < -canvasRectTransform.rect.height / 2)
        {
            anchoredPosition.y = -canvasRectTransform.rect.height / 2 + tooltipRectTransform.rect.height / 2;
        }
        tooltipRectTransform.anchoredPosition = anchoredPosition;
    }

    private void OnDestroy()
    {
        EventBus.StopListening<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, ShowTooltip);
        EventBus.StopListening(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT, HideTooltip);
    }
}


