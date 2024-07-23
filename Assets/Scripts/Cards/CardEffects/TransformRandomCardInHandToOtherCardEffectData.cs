using UnityEngine;

[CreateAssetMenu(fileName = "TransformRandomCardInHandToStoneCardEffectData", menuName = "Data/Card Effects/Transform Random Card To Stone")]
public class TransformRandomCardInHandToOtherCardEffectData : CardEffectData
{
    public CardData cardData;

    public override CardEffect CreateEffect()
    {
        return new TransformRandomCardInHandToOtherCardEffect(this);
    }
}