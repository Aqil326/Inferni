using UnityEngine;

[CreateAssetMenu(fileName = "AddDamageDealtModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Damage Dealt")]
public class AddDamageDealtModifierEffectData : AddCharacterModifierEffectData
{
    public float damageMultiplier = 1;
    public int damageAddition;

    public override Modifier GetModifier()
    {
        return new DamageSentModifier(damageMultiplier,
                                        damageAddition,
                                        InternalID,
                                        modifierData,
                                        duration,
                                        useProjectileLifetimeMultiplier);
    }

}


