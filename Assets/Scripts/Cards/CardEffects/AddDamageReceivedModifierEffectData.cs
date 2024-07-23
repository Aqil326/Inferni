using UnityEngine;

[CreateAssetMenu(fileName = "AddDamageReceivedModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Damage Received")]
public class AddDamageReceivedModifierEffectData : AddCharacterModifierEffectData
{
    public float damageMultiplier = 1;
    public int damageReduction;

    public override Modifier GetModifier()
    {
        return new DamageReceivedModifier(damageMultiplier,
                                        damageReduction,
                                        InternalID,
                                        modifierData,
                                        duration,
                                        useProjectileLifetimeMultiplier);
    }

}


