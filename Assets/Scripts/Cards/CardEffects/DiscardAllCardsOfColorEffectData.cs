using UnityEngine;

[CreateAssetMenu(fileName = "DiscardAllCardsOfColorEffectData", menuName = "Data/Card Effects/Discard All Cards Of Color")]
public class DiscardAllCardsOfColorEffectData : CardEffectData
{
    public CardColors Color;

    public override CardEffect CreateEffect()
    {
        return new DiscardAllCardsOfColorEffect(this);
    }
}


