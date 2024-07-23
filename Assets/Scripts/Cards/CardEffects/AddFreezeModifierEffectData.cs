using UnityEngine;

[CreateAssetMenu(fileName = "AddFreezeModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Freeze")]
public class AddFreezeModifierEffectData : AddCharacterModifierEffectData
{
    public override CardEffect CreateEffect()
    {
        return new AddFreezeModifierEffect(this);
    }

    public override Modifier GetModifier()
    {
        return new FreezeModifier(InternalID, modifierData, duration, useProjectileLifetimeMultiplier);
    }

}


