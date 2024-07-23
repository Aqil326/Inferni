using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CastCostUI :MonoBehaviour
{
    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private TextMeshProUGUI costAmount;

    private GlobalGameSettings gameSettings;

    public void SetCost(EnergyCost cost)
    {
        if(gameSettings == null)
        {
            gameSettings = GlobalGameSettings.Settings;
        } 
        
        backgroundImage.color = gameSettings.GetCardColor(cost.color);
        switch (cost.color)
        {
            case CardColors.White:
                costAmount.color = Color.black;
                break;
            case CardColors.Black:
                costAmount.color = Color.white;
                break;
            case CardColors.Blue:
                costAmount.color = Color.white;
                break;
            case CardColors.Green:
                costAmount.color = Color.white;
                break;
            case CardColors.Red:
                costAmount.color = Color.white;
                break;
        }
        costAmount.text = cost.cost.ToString();
    }
}
