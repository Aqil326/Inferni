
using UnityEngine;

[CreateAssetMenu(fileName = "ChangeCastingTimerEffectData", menuName = "Data/Card Effects/Change Casting Timer")]
public class ChangeCastingTimerEffectData : CardEffectData
{
    public int castTimerAddition;
    public float castTimerMultiplier = 1;

    public override CardEffect CreateEffect()
    {
        return new ChangeCastingTimerEffect(this);
    }
}
