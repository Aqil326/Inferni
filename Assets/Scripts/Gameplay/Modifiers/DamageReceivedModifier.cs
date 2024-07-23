using UnityEngine;

public class DamageReceivedModifier : CharacterModifier
{
    private float damageMultiplier = 1;
    private int damageReduction = 0;

    public DamageReceivedModifier(float damageMultiplier,
                                    int damageReduction,
                                    string id,
                                    ModifierData modifierData,
                                    float duration,
                                    bool useProjectileLifetimeMultiplier) :
                                    base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    { 
        this.damageMultiplier = damageMultiplier;
        this.damageReduction = damageReduction;
    }

    public int ModifyDamage(int receivedDamage)
    {
        int damage = ((int)(receivedDamage * damageMultiplier)) - damageReduction;
        if(damage < 0)
        {
            damage = 0;
        }
        return damage;
    }
}
