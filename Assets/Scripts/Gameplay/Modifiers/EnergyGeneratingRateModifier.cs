using UnityEngine;

public class EnergyGeneratingRateModifier : CharacterModifier
{
    private float rateMultiplier = 1;

    public EnergyGeneratingRateModifier(float rateMultiplier,
                                    string id,
                                    ModifierData modifier,
                                    float duration,
                                    bool useProjectileLifetimeMultiplier) :
                                    base(id, modifier, duration, useProjectileLifetimeMultiplier)
    {
        this.rateMultiplier = rateMultiplier;
    }

    public float ModifyGeneratingTime(float initialTime)
    {
        return initialTime * rateMultiplier;
    }
}
