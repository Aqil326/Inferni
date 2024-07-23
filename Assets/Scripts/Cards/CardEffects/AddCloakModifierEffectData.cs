using UnityEngine;

[CreateAssetMenu(fileName = "AddCloakModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Cloak")]
public class AddCloakModifierEffectData : AddCharacterModifierEffectData
{
    public Material cloakedMaterial;

    public override Modifier GetModifier()
    {
        return new CloakModifier(InternalID, cloakedMaterial, modifierData, duration, useProjectileLifetimeMultiplier);
    }
}