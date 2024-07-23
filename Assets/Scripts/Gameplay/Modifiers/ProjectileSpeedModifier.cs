using UnityEngine;

public class ProjectileSpeedModifier : CharacterModifier
{
    private float speedMultiplier = 1;
    private int numberOfProjectilesAffected;

    private int projectilesFired;

    public ProjectileSpeedModifier(float speedMultiplier,
                                    int numberOfProjectilesAffected,
                                    string id,
                                    ModifierData modifierData,
                                    float duration,
                                    bool useProjectileLifetimeMultiplier) :
                                    base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.speedMultiplier = speedMultiplier;
        this.numberOfProjectilesAffected = numberOfProjectilesAffected;

        if (numberOfProjectilesAffected > 0)
        {
            Character.ProjectileFiredEvent_Server += OnProjectileFired;
        }
    }

    public float GetProjectileSpeed(float initialSpeed)
    {
        return initialSpeed * speedMultiplier;
    }

    private void OnProjectileFired(Projectile projectile)
    {
        projectilesFired++;

        if(projectilesFired >= numberOfProjectilesAffected)
        {
            RemoveModifier();
        }
    }

    public override void RemoveModifier()
    {
        Character.ProjectileFiredEvent_Server += OnProjectileFired;
        base.RemoveModifier();
    }
}
