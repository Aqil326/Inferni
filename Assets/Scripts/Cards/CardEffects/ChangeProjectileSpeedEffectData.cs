using UnityEngine;

[CreateAssetMenu(fileName = "ChangeProjectileSpeedEffectData", menuName = "Data/Card Effects/Change Projectile Speed")]
public class ChangeProjectileSpeedEffectData : CardEffectData
{
    public int speedMultiplier;

    public override CardEffect CreateEffect()
    {
        return new ChangeProjectileSpeedEffect(this);
    }
}
