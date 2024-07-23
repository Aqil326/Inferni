using UnityEngine;

[CreateAssetMenu(fileName = "AddMirrorModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Mirror")]
public class AddMirrorModifierEffectData : AddCharacterModifierEffectData
{
    public override Modifier GetModifier()
    {
        return new MirrorModifier(InternalID, modifierData, duration, useProjectileLifetimeMultiplier);
    }
}


