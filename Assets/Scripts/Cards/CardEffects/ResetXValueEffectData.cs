using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResetXValueEffectData", menuName = "Data/Card Effects/Reset X Value")]
public class ResetXValueEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new ResetXValueEffect(this);
    }
}
