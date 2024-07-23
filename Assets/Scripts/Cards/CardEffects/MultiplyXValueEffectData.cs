using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MultiplyXValueEffectData", menuName = "Data/Card Effects/Multiply X Value")]
public class MultiplyXValueEffectData : CardEffectData
{
    public float multiplier;

    public override CardEffect CreateEffect()
    {
        return new MultiplyXValueEffect(this);
    }
}
