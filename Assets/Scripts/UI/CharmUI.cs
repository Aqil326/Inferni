using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharmUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Image charmIcon;

    [SerializeField]
    private TextMeshProUGUI charmText;

    [SerializeField]
    private GameObject selectedHighlight;

    public Charm Charm { get; private set; }

    private bool isPointerOver;

    public void SetCharm(Charm charm)
    {
        selectedHighlight.SetActive(false);
        Charm = charm;
        charmIcon.sprite = charm.CharmData.charmIcon;
        charmText.text = charm.CharmData.charmName;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        if(Charm == null)
        {
            return;
        }

        var charmData = Charm.CharmData;
        EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, new TooltipData()
        {
            Title = charmData.charmName,
            Description = charmData.description,
            IconImage = charmData.charmIcon,
        });
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ClearTooltip();
    }

    private void ClearTooltip()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);
    }

    public void Select()
    {
        selectedHighlight.SetActive(true);
    }

    public void Deselect()
    {
        selectedHighlight.SetActive(false);
    }

    public void TryClearTooltip()
    {
        if(isPointerOver)
        {
            ClearTooltip();
            isPointerOver = false;
        }
    }
}
