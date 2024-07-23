using UnityEngine;

[CreateAssetMenu(fileName = "EmptyAllEnergyPoolsEffectData", menuName = "Data/Card Effects/Empty All Energy Pools")]
public class EmptyAllEnergyPoolsEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new EmptyAllEnergyPoolsEffect(this);
    }
}




