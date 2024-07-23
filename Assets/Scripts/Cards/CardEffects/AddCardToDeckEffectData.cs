using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddCardToDeckEffectData", menuName = "Data/Card Effects/Add Card To Deck")]
public class AddCardToDeckEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new AddCardToDeckEffect(this);
    }
}
