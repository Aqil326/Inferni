using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnergyCost
{
    public CardColors color;
    public int cost;
}

[CreateAssetMenu(fileName = "CardData", menuName = "Data/Card")]
public class CardData : ScriptableObjectWithId
{
    public string Name;

    public int MaxAllowableHPForCast = 0;
    public bool IsBlocked = false;
    public List<EnergyCost> EnergyCost;
    public CardColors CardColor;
    public CardTypes CardType;
    public List<CardSubTypes> CardSubtypes;
    public float CastingTime;
    public float ProjectileSpeedMultiplier = 1;
    public float EnergyCastMultiplier = 1;

    public CardTarget Target;

    public Sprite CardSprite;

    [TextArea]
    public string CardText;

    public SoundManager.Sound EndCastSound;
    public SoundManager.Sound ProjectileHitSound;
    public SoundManager.Sound ProjectileTravelSound;

    public Projectile.ProjectileType ProjectileType = Projectile.ProjectileType.Damage;

    public List<CardEffectData> InstantEffectDatas;

    public List<CardEffectData> OnCastEffectDatas;

    public List<CardEffectData> ProjectileEffectDatas;

    public List<CardEffectData> EnergyPoolSacrificeEffectDatas;

    public int BaseScore;
}
