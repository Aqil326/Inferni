using UnityEngine;

public class ChangeProjectilesSpeedGlobalModifier : GlobalModifier
{
    private float projectileSpeedMultiplier;
    private CardsEffectsManager cardsEffectsManager;

    public ChangeProjectilesSpeedGlobalModifier(float projectileSpeedMultiplier,
                                                string id,
                                                ModifierData modifierData,
                                                float duration,
                                                bool useProjectileLifetimeMultiplier) :
                                                base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.projectileSpeedMultiplier = projectileSpeedMultiplier;
        cardsEffectsManager = GameManager.GetManager<CardsEffectsManager>();
    }

    public override void AttachToWorld(World world)
    {
        base.AttachToWorld(world);

        foreach(var projectile in cardsEffectsManager.Projectiles)
        {
            projectile.AddSpeedMultiplier(projectileSpeedMultiplier);
        }

        EventBus.StartListening<Projectile>(EventBusEnum.EventName.PROJECTILE_SHOT_SERVER, OnProjectileSpawned);
    }

    private void OnProjectileSpawned(Projectile projectile)
    {
        projectile.AddSpeedMultiplier(projectileSpeedMultiplier);
    }

    public override void RemoveModifier()
    {
        base.RemoveModifier();
        EventBus.StopListening<Projectile>(EventBusEnum.EventName.PROJECTILE_SHOT_SERVER, OnProjectileSpawned);

        foreach (var projectile in cardsEffectsManager.Projectiles)
        {
            projectile.AddSpeedMultiplier(1/projectileSpeedMultiplier);
        }
    }
}
