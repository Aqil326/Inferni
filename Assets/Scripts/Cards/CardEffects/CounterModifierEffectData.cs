using UnityEngine;

[CreateAssetMenu(fileName = "CounterModifierEffectData", menuName = "Data/Card Effects/Counter Modifier")]
public class CounterModifierEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new CounterModifierEffect(this);
    }
}
