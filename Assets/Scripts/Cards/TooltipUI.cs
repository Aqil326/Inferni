using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct TooltipData
{
    public string Title;
    public string Description;
    public Sprite IconImage;
}

public class TooltipUI: MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [SerializeField]
    private TextMeshProUGUI titleText;
    
    [SerializeField] 
    private IconWithMaskController icon;

    [SerializeField]
    private GameObject iconParent;

    private TooltipData tooltipData;

    private bool isShowing;

    public void Show(TooltipData tooltipData)
    {
        this.tooltipData = tooltipData;
        gameObject.SetActive(true);
        descriptionText.text = tooltipData.Description;
        titleText.text = tooltipData.Title;

        iconParent.SetActive(tooltipData.IconImage != null);
        icon.InitIcon(tooltipData.IconImage);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
        titleText.text = "";
        descriptionText.text = "";
        icon.InitIcon(null);
    }
}


