using UnityEngine;

[CreateAssetMenu(fileName = "DiscardAllCardsAndDrawBackEffectData", menuName = "Data/Card Effects/Discard all Cards and Draw Back")]
public class DiscardAllCardsAndDrawBackEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new DiscardAllCardsAndDrawBackEffect(this);
    }
}




