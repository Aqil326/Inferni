using UnityEngine;

[CreateAssetMenu(fileName = "AddEnergyEffectData", menuName = "Data/Card Effects/Add Energy Effect")]
public class AddEnergyEffectData : CardEffectData
{
    public int energyAmount;
    public bool maxEnergy;
    public bool useColor;
    public CardColors energyColor;

    public override CardEffect CreateEffect()
    {
        return new AddEnergyEffect(this);
    }
}
