using System;
using Unity.Netcode;
using UnityEngine;

public abstract class AddModifierEffectData : CardEffectData
{
    public float duration;
    public bool useProjectileLifetimeMultiplier;
    public ModifierData modifierData;

    public abstract Modifier GetModifier();

}




