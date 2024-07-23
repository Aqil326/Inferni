using UnityEngine;

public class CharacterModifier : Modifier
{
    public Character Character;

    public CharacterModifier(string id, ModifierData modifierData, float duration, bool useProjectileLifetimeMultiplier) : base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {

    }

    public virtual void AttachToCharacter(Character character)
    {
        Character = character;
        Init(character);
    }

    public virtual void ApplyVisualEffects(CharacterView characterView, bool pause)
    {
        if (characterView != null && Data.modifierEffectPrefab != null)
        {
            characterView.AddModifierEffect(Data.modifierEffectPrefab);
            if (pause) characterView.Pause(true);
        }
    }

    public virtual void RemoveVisualEffects(CharacterView characterView, bool pause)
    {
        if (characterView != null)
        {
            characterView.RemoveModifierEffect();
            if (pause) characterView.Pause(false);
        }
    }

    public override void RemoveModifier()
    {
        base.RemoveModifier();
        Character.CharacterView.RemoveModifierEffect();
    }
}
