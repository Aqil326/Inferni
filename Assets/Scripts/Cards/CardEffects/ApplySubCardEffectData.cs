using UnityEngine;

[CreateAssetMenu(fileName = "ApplySubCardEffectData", menuName = "Data/Card Effects/Apply Sub Card Effects")]
public class ApplySubCardEffectData : CardEffectData
{
    public CardData cardData;

    public override CardEffect CreateEffect()
    {
        return new ApplySubCardEffect(this);
    }
}
