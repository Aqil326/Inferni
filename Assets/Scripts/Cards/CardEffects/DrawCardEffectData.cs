using UnityEngine;

[CreateAssetMenu(fileName = "DrawCardEffectData", menuName = "Data/Card Effects/Draw Cards")]
public class DrawCardEffectData : CardEffectData
{
    public int cardsAmount;

    public override CardEffect CreateEffect()
    {
        return new DrawCardEffect(this);
    }
}
