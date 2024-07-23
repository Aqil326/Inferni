using UnityEngine;

public class DamageSentModifier : CharacterModifier
{
    private float damageMultiplier = 1;
    private int damageAddition = 0;

    public DamageSentModifier(float damageMultiplier,
                                int damageAddition,
                                string id,
                                ModifierData modifierData,
                                float duration,
                                bool useProjectileLifetimeMultiplier) :
                                base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.damageMultiplier = damageMultiplier;
        this.damageAddition = damageAddition;
    }

    public int ModifyDamage(int initialDamage)
    {
        int damage = ((int)(initialDamage * damageMultiplier)) + damageAddition;
        if(damage < 0)
        {
            damage = 0;
        }
        return damage;
    }
}
