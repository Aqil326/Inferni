
using UnityEngine;

public class GlobalModifier : Modifier
{
    public World World;

    public GlobalModifier(string id, ModifierData data, float duration, bool useProjectileLifetimeMultiplier) : base(id, data, duration, useProjectileLifetimeMultiplier)
    {
    }

    public virtual void AttachToWorld(World world)
    {
        World = world;
        Init(world);
    }

    public virtual void ApplyVisualEffects(WorldView worldView)
    {
        if (worldView != null && Data.modifierEffectPrefab != null)
        {
            worldView.AddModifierEffect(Data.modifierEffectPrefab);
        }
    }

    public virtual void RemoveVisualEffects(WorldView worldView)
    {
        if (worldView != null)
        {
            worldView.RemoveModifierEffect();
        }
    }

}
