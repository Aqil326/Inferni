using UnityEngine;

public class AddAdditionalDamageModifierEffect : AddCharacterModifierEffect
{
    public AddAdditionalDamageModifierEffect(AddAdditionalDamageModifierData data) : base(data)
    {
        
    }

    protected override bool ShouldApplyExtraProjectileEffects(ITargetable target, ref int damage)
    {
        return false;
    }
}

