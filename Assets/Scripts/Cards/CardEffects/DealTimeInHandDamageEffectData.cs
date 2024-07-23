using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DealTimeInHandDamageEffectData", menuName = "Data/Card Effects/Deal Time In Hand Damage")]
public class DealTimeInHandDamageEffectData  : CardEffectData
{
    public int DelayTime;
    public int MaxDamageValue;
    public override CardEffect CreateEffect()
    {
        return new DealTimeInHandDamageEffect(this);
    }
}
