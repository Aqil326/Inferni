using UnityEngine;

[CreateAssetMenu(fileName = "SetToCastingHealthEffectData", menuName = "Data/Card Effects/Set To Casting Health")]
public class SetToCastingHealthEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new SetToCastingHealthEffect(this);
    }
}


