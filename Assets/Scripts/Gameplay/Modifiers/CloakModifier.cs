using UnityEngine;

public class CloakModifier : CharacterModifier
{
    private Material cloakMaterial;

    public CloakModifier(string id,
                        Material cloakMaterial,
                        ModifierData modifierData,
                        float duration,
                        bool useProjectileLifetimeMultiplier) :
        base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.cloakMaterial = cloakMaterial;
    }

    public override void ApplyVisualEffects(CharacterView characterView, bool pause)
    {
        base.ApplyVisualEffects(characterView, pause);
        if (characterView != null)
        {
            characterView.CharacterModel.ChangeMaterial(cloakMaterial);
        }
    }

    public override void RemoveVisualEffects(CharacterView characterView, bool pause)
    {
        base.RemoveVisualEffects(characterView, pause);
        if (characterView != null)
        {
            characterView.CharacterModel.ChangeMaterialToOriginal();
        }
    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.SetTargetable(false);
    }

    public override void RemoveModifier()
    {
        base.RemoveModifier();
        Character.SetTargetable(true);
    }
}
