using UnityEngine;

[CreateAssetMenu(fileName = "AddAdditionalDamageModifierData", menuName = "Data/Card Effects/Add Modifiers/Additional Damage")]
public class AddAdditionalDamageModifierData : AddCharacterModifierEffectData
{
    public int damage;
    public string cardDescription;
    
    public override CardEffect CreateEffect()
    {
        return new AddAdditionalDamageModifierEffect(this);
    }

    public override Modifier GetModifier()
    {
        return new AdditionalDamageModifier(damage, cardDescription, InternalID, modifierData, duration, useProjectileLifetimeMultiplier);
    }
}


