using UnityEngine;

[CreateAssetMenu(fileName = "AddExtraEffectToCardModifierData", menuName = "Data/Card Effects/Add Modifiers/Extra Effect to Card")]
public class AddExtraEffectToCardModifierData : AddCharacterModifierEffectData
{
    public CardEffectData extraEffectData;
    public bool isProjectileEffect;
    public bool isInstantEffect;

    public override CardEffect CreateEffect()
    {
        return new AddExtraEffectToCardModifierEffect(this);
    }

    public override Modifier GetModifier()
    {
        return new AddExtraEffectToCardModifier(extraEffectData, isProjectileEffect, isInstantEffect, InternalID, modifierData, duration, useProjectileLifetimeMultiplier);
    }
}


