using UnityEngine;

[CreateAssetMenu(fileName = "DiscardRandomCardEffectData", menuName = "Data/Card Effects/Discard Random Card")]
public class DiscardRandomCardEffectData : CardEffectData
{
    public int cardsAmount = 1;
    public override CardEffect CreateEffect()
    {
        return new DiscardRandomCardEffect(this);
    }
}
