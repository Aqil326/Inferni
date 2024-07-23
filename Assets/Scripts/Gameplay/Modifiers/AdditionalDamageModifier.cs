using UnityEngine;

public class AdditionalDamageModifier : CharacterModifier
{
    private int damage;
    private string cardDescription;
    public AdditionalDamageModifier(int damage,
                        string cardDescription,
                        string id,
                        ModifierData modifierData,
                        float duration,
                        bool useProjectileLifetimeMultiplier) :
                        base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.damage = damage;
        this.cardDescription = cardDescription;
    }

    public int GetDamageAmount()
    {
        return damage;
    }
}
