using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReturnSpellToHandEffectData", menuName = "Data/Card Effects/Return Spell To Hand")]
public class ReturnSpellToHandEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new ReturnSpellToHandEffect(this);
    }
}
