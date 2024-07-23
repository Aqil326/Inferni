using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ModifierUI : MonoBehaviour
{
    [SerializeField] 
    private IconWithMaskController icon;

    [SerializeField]
    private Image modifierTimer;

    [SerializeField]
    private GameObject modifierArea;

    [SerializeField]
    private Image frameImage;

    public ModifierData ModifierData { get; private set; }
    
    private bool isPointerOver;
    [SerializeField]
    private TextMeshProUGUI timeText;

    private float modifierDuration;
    private NetworkVariable<float> modifierTimeRemaining;

    public void AddModifier(ModifierData modifier, float duration, NetworkVariable<float> timer)
    {
        ModifierData = modifier;
        icon.InitIcon(ModifierData.modifierIcon);
        if(modifierTimeRemaining != null)
        {
            modifierTimeRemaining.OnValueChanged -= UpdateTimer;
        }
        modifierDuration = duration;
        modifierTimeRemaining = timer;
        modifierTimeRemaining.OnValueChanged += UpdateTimer;
        modifierArea.SetActive(true);
        frameImage.gameObject.SetActive(true);
    }

    private void UpdateTimer(float previous, float current)
    {
        modifierTimer.fillAmount = 1 - (current/modifierDuration);

        timeText.text = string.Format("{0}s", (int)(modifierDuration - current));
    }

    public void Clear()
    {
        modifierArea.SetActive(false);
        frameImage.gameObject.SetActive(false);
        ClearPreviewText();
        ModifierData = null;
    }
    
    public void OnPointerEnter()
    {
        if(ModifierData == null)
        {
            return;
        }

        isPointerOver = true;
        TooltipData tooltipData = new TooltipData()
        {
            Title = ModifierData.modifierName,
            Description = ModifierData.description,
            IconImage = ModifierData.modifierIcon,
        };
        EventBus.TriggerEvent<TooltipData>(EventBusEnum.EventName.SHOW_TOOLTIP_CLIENT, tooltipData);
    }

    public void OnPointerExit()
    {
        ClearPreviewText();
        isPointerOver = false;
    }

    private void OnDisable()
    {
        ClearPreviewText();
    }

    private void ClearPreviewText()
    {
        if (isPointerOver)
        {
            EventBus.TriggerEvent(EventBusEnum.EventName.CLEAR_TOOLTIP_CLIENT);    
        }
    }
}
