public abstract class OnProjectileHitModifier : CharacterModifier
{
    public OnProjectileHitModifier(string id,
                                    ModifierData modifierData,
                                    float duration,
                                    bool useProjectileLifetimeMultiplier) :
                                    base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
    }

    public abstract bool OnProjectileHit(Projectile projectile, Character character);
}
