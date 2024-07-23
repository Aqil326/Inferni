using System.Collections.Generic;
using UnityEngine;




public enum CardEffectCategory
{
    Damage = 1 << 0,
    Healing = 1 << 1,
    SelfHeal = 1 << 2,
    Draw = 1 << 3,
    Discard = 1 << 4,
    Buff = 1 << 5,
    Debuff = 1 << 6,
    Revive = 1 << 7,
    Defense = 1 << 8,
    Support = 1 << 9,
    Disruption = 1 << 10,
    SelfDamage = 1 << 11,
    HealthManipulation = 1 << 12,
    CharmManipulation = 1 << 13,
    Other = 1 << 14
}

public abstract class CardEffectData : ScriptableObjectWithId
{
    public CardEffectTarget target;
    public List<CardEffectCategory> CardEffectCategories;
    [TextArea]
    public string effectDescription;

    public abstract CardEffect CreateEffect();
}
