using UnityEngine;

[CreateAssetMenu(fileName = "ChangeTargetedProjectilesTargetsEffectData", menuName = "Data/Card Effects/Change Targeted Projectiles Targets")]
public class ChangeTargetedProjectilesTargetsEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new ChangeTargetedProjectilesTargetsEffect(this);
    }
}
