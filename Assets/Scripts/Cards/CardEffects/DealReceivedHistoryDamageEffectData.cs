using UnityEngine;

[CreateAssetMenu(fileName = "DealReceivedHistoryDamageEffectData", menuName = "Data/Card Effects/Deal Received History Damage")]
public class DealReceivedHistoryDamageEffectData : CardEffectData
{
    public int time;

    public override CardEffect CreateEffect()
    {
        return new DealReceivedHistoryDamageEffect(this);
    }
}