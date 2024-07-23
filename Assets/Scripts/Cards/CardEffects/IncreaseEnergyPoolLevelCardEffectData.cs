using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseEnergyPoolLevelCardEffectData", menuName = "Data/Card Effects/Increase Energy Pool Level")]
public class IncreaseEnergyPoolLevelCardEffectData : CardEffectData
{
    public int numberOfLevels = 1;

    public override CardEffect CreateEffect()
    {
        return new IncreaseEnergyPoolLevelCardEffect(this);
    }
}
