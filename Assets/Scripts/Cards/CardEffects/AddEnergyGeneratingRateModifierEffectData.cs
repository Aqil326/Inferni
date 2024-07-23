using UnityEngine;

[CreateAssetMenu(fileName = "AddEnergyGeneratingRateModifierEffectData", menuName = "Data/Card Effects/Add Modifiers/Energy Generating Rate")]
public class AddEnergyGeneratingRateModifierEffectData : AddCharacterModifierEffectData
{
    public float rateMultiplier = 1;

    public override Modifier GetModifier()
    {
        return new EnergyGeneratingRateModifier(rateMultiplier,
                                        InternalID,
                                        modifierData,
                                        duration,
                                        useProjectileLifetimeMultiplier);
    }

}




