using UnityEngine;

[CreateAssetMenu(fileName = "ChangeProjectileSpeedCharmData", menuName = "Data/Charms/Change Projectile Speed")]
public class ChangeProjectileSpeedCharmData: CharmData
{
    public float ProjectileSpeedMultiplier;
    
    public override Charm GetCharm()
    {
        return new ChangeProjectileSpeedCharm(this);
    }
}