using UnityEngine;

[CreateAssetMenu(fileName = "DealDealtHistoryDamageEffectData", menuName = "Data/Card Effects/Deal Dealt History Damage")]
public class DealDealtHistoryDamageEffectData : CardEffectData
{
    public int time;

    public override CardEffect CreateEffect()
    {
        return new DealDealtHistoryDamageEffect(this);
    }
}