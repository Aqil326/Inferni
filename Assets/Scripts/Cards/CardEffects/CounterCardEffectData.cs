using UnityEngine;

[CreateAssetMenu(fileName = "CounterCardEffectData", menuName = "Data/Card Effects/Counter Card")]
public class CounterCardEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new CounterCardEffect(this);
    }
}
