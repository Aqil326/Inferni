using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DealSpellAmountDamageEffectData", menuName = "Data/Card Effects/Deal Spell Amount Damage")]
public class DealSpellAmountDamageEffectData : CardEffectData
{
    public int time;

    public override CardEffect CreateEffect()
    {
        return new DealSpellAmountDamageEffect(this);
    }
}