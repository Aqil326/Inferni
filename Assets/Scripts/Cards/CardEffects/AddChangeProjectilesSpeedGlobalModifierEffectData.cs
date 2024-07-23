using UnityEngine;

[CreateAssetMenu(fileName = "AddChangeProjectilesSpeedGlobalModifierEffectData", menuName = "Data/Card Effects/Add Global Modifiers/Change Projectiles Speed")]
public class AddChangeProjectilesSpeedGlobalModifierEffectData : AddGlobalModifierEffectData
{
    public float projectileSpeedMultiplier = 1f;

    public override Modifier GetModifier()
    {
        return new ChangeProjectilesSpeedGlobalModifier(projectileSpeedMultiplier, InternalID, modifierData, duration, useProjectileLifetimeMultiplier);
    }
}


