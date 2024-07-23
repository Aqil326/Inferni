using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public struct XData : INetworkSerializable
{
    public bool IsVisible;
    public int Value;
    public CardEffectCategory EffectType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref IsVisible);
        serializer.SerializeValue(ref Value);
        serializer.SerializeValue(ref EffectType);
    }
}

[Serializable]
public class Card : ITargetable
{
    public static string PROJECTILE_EFFECT_PREFIX = "On impact: ";
    public static string INSTANT_EFFECT_PREFIX = "Start cast: ";
    public static string CAST_EFFECT_PREFIX = "On cast: ";
    public static string SACRIFICE_POOL_EFFECT_PREFIX = "On sacrifice to energy pool: ";
    public static string PROJECTILE_SPEED_TEXT = "Projectile speed is {X}x";
    public static string MAX_HEALTH_TO_CAST_TEXT = "Can only be cast when your life is less than {X}";
    public static string CARD_TAG = "#Card#";

    public CardData Data { get; private set; }
    public Character Owner { get; private set; }
    public Character OriginalOwner { get; private set; }
    public List<CardEffect> ProjectileCardEffects { get; private set; } = new List<CardEffect>();
    public List<CardEffect> OnCastCardEffects { get; private set; } = new List<CardEffect>();
    public List<CardEffect> InstantCardEffects { get; private set; } = new List<CardEffect>();
    public List<CardEffect> EnergyPoolSacrificeCardEffects { get; private set; } = new List<CardEffect>();
    public Projectile Projectile { get; private set; }
    
    public CardUI CardUI { get; protected set; }
    public XData XData;

    public float ProjectileSpeed => Owner.GetProjectileSpeed(GlobalGameSettings.Settings.GetProjectileSpeed(GameManager.Instance.ArenaGroup)) * Data.ProjectileSpeedMultiplier;
    public float TimeInHand { get; set; }

    public int HandIndex { get; private set; } = -1;
    string ITargetable.TargetId => $"{Owner.PlayerData.Id}{CARD_TAG}{HandIndex}";
    Vector3 ITargetable.Position => Owner.transform.position;

    private CardsEffectsManager effectsManager;
    
    public Card(CardData data)
    {
        Data = data;
        effectsManager = GameManager.GetManager<CardsEffectsManager>();

        foreach (var effectData in Data.ProjectileEffectDatas)
        {
            var eff = effectData.CreateEffect();
            eff.Init(this);
            ProjectileCardEffects.Add(eff);
        }

        foreach (var effectData in Data.InstantEffectDatas)
        {
            var eff = effectData.CreateEffect();
            eff.Init(this);
            InstantCardEffects.Add(eff);
        }

        foreach (var effectData in Data.OnCastEffectDatas)
        {
            var eff = effectData.CreateEffect();
            eff.Init(this);
            OnCastCardEffects.Add(eff);
        }

        foreach (var effectData in Data.EnergyPoolSacrificeEffectDatas)
        {
            var eff = effectData.CreateEffect();
            eff.Init(this);
            EnergyPoolSacrificeCardEffects.Add(eff);
        }
    }

    public void SetHandIndex(int handIndex)
    {
        this.HandIndex = handIndex;
    }

    public void UpdateTimeInHand(float deltaTime)
    {
        TimeInHand += deltaTime;
    }

    public void SetOwner(Character newOwner)
    {
        Owner = newOwner;
        
        if (OriginalOwner == null)
        {
            OriginalOwner = newOwner;
        }
        
        foreach (var effect in ProjectileCardEffects)
        {
            effect.SetOwner(Owner);
        }

        foreach (var effect in InstantCardEffects)
        {
            effect.SetOwner(Owner);
        }

        foreach (var effect in OnCastCardEffects)
        {
            effect.SetOwner(Owner);
        }

        foreach (var effect in EnergyPoolSacrificeCardEffects)
        {
            effect.SetOwner(Owner);
        }
    }

    public void SetCardUI(CardUI cardUI)
    {
        CardUI = cardUI;
    }

    public void SetCastCostUIs(CastCostUI[] castCostUIs)
    {
        for (int i = 0; i < castCostUIs.Length; i++)
        {
            if (i < Data.EnergyCost.Count)
            {
                castCostUIs[i].gameObject.SetActive(true);
                castCostUIs[i].SetCost(Data.EnergyCost[i]);
            }
            else
            {
                castCostUIs[i].gameObject.SetActive(false);
            }
        }
    }

    public float GetCardCastingTime()
    {
        var originalCastingTime = Data.CastingTime;
        if (Owner == null) return originalCastingTime;
        
        var castingTimeChange = Owner.CastingTimeChange.Value;
        if (castingTimeChange == 0) return originalCastingTime;
        
        var newCastingTime = Data.CastingTime + castingTimeChange;
        return newCastingTime > 0 ? newCastingTime : 0;
    }
    
    public string GetCardText()
    {
        string text = "";

        if(InstantCardEffects.Count > 0)
        {
            text += INSTANT_EFFECT_PREFIX;
            foreach (var e in InstantCardEffects)
            {
                text += e.GetEffectCardText() +".";
            }
            text += "\n";
        }

        if (OnCastCardEffects.Count > 0)
        {
            text += CAST_EFFECT_PREFIX;
            foreach (var e in OnCastCardEffects)
            {
                text += e.GetEffectCardText() + ".";
            }
            text += "\n";
        }

        if (ProjectileCardEffects.Count > 0)
        {
            text += PROJECTILE_EFFECT_PREFIX;
            foreach (var e in ProjectileCardEffects)
            {
                var effectText = e.GetEffectCardText();
                if (!string.IsNullOrEmpty(effectText))
                {
                    text += effectText + ".";    
                }
            }
            text += "\n";
        }

        if (EnergyPoolSacrificeCardEffects.Count > 0)
        {
            text += SACRIFICE_POOL_EFFECT_PREFIX;
            foreach (var e in EnergyPoolSacrificeCardEffects)
            {
                text += e.GetEffectCardText() + ".";
            }
            text += "\n";
        }

        if(Data.ProjectileSpeedMultiplier != 1)
        {
            text += PROJECTILE_SPEED_TEXT.Replace("{X}", Data.ProjectileSpeedMultiplier.ToString()) + ".\n";
        }

        if(Data.MaxAllowableHPForCast > 0)
        {
            text += MAX_HEALTH_TO_CAST_TEXT.Replace("{X}", Data.MaxAllowableHPForCast.ToString()) + ".\n";
        }

        text += Data.CardText;

        return text;
    }

    public List<CardEffect> GetAllCardEffects()
    {
        var list = new List<CardEffect>();
        list.AddRange(EnergyPoolSacrificeCardEffects);
        list.AddRange(InstantCardEffects);
        list.AddRange(OnCastCardEffects);
        list.AddRange(ProjectileCardEffects);
        return list;
    }

    public bool HasEnoughEnergy(Character character)
    {
        foreach (var cost in Data.EnergyCost)
        {
            var allEnergyForColor = character.EnergyPools.Where(pool => pool.Color.Value == cost.color).Sum(pool => pool.Energy.Value);
            if (allEnergyForColor < cost.cost) return false;
        }
        return true;
    }

    public bool IsTargetViewValid(TargetableView target)
    {
        if(target == null)
        {
            return false;
        }

        bool canBeTargeted = true;
        if (target.ShouldOverrideTargetAvailability(ref canBeTargeted, this))
        {
            return canBeTargeted;
        }

        return IsTargetableValid(target.Targetable);
    }

    public bool IsTargetableValid(ITargetable targetable)
    {
        if (targetable == null) return false;

        var targetables = effectsManager.ChooseCardsTargets(Owner, targetable, this);
        return targetables.Count > 0;
    }
    
    public bool IsCastable()
    {
        var castableHealth = Data.MaxAllowableHPForCast;
        if (castableHealth > 0 && Owner != null)
        {
            return castableHealth >= Owner.Health.Value;
        }
        return true;
    }

    public void ApplyEnergyPoolSacrificeEffects(Character sourceCharacter)
    {
        if(EnergyPoolSacrificeCardEffects.Count > 0)
        {
            effectsManager.ApplyCardEffects(sourceCharacter, this, EnergyPoolSacrificeCardEffects, null);
        }
    }
    
    public void ApplyStartCastEffects(Character sourceCharacter, ITargetable target)
    {
        if(InstantCardEffects.Count > 0)
        {
            effectsManager.ApplyCardEffects(sourceCharacter, this, InstantCardEffects, target);
        }
    }
    
    public void PlayCard(Character sourceCharacter, ITargetable target)
    {
        if(OnCastCardEffects.Count > 0)
        {
            effectsManager.ApplyCardEffects(sourceCharacter, this, OnCastCardEffects, target);
        }

        if (ProjectileCardEffects.Count > 0)
        {
            effectsManager.TryCreateProjectiles(sourceCharacter, this, target);
        }
    }

    public void SetProjectile(Projectile projectile)
    {
        Projectile = projectile;
        foreach (var eff in this.ProjectileCardEffects)
        {
            eff.SetProjectile(projectile);
        }
    }

    public bool TryUpdateCardXData(CardXPreviewType previewType, Character targetCharacter = null)
    {
        var xData = CalculateXData(previewType, targetCharacter);
        if(XData.Equals(xData))
        {
            return false;
        }
        XData = xData;
        return true;
    }

    public XData CalculateXData(CardXPreviewType previewType, Character targetCharacter = null)
    {
        var xData = new XData();
        foreach (var eff in this.InstantCardEffects)
        {
            eff.UpdateXData(this, previewType, targetCharacter);
            if(xData.EffectType != eff.XData.EffectType)
            {
                xData.Value = eff.XData.Value;
            }
            else
            {
                xData.Value += eff.XData.Value;
            }
            xData.IsVisible = eff.XData.IsVisible;
            xData.EffectType = eff.XData.EffectType;
        }

        foreach (var eff in this.ProjectileCardEffects)
        {
            eff.UpdateXData(this, previewType, targetCharacter);
            if (xData.EffectType != eff.XData.EffectType)
            {
                xData.Value = eff.XData.Value;
            }
            else
            {
                xData.Value += eff.XData.Value;
            }
            xData.IsVisible = eff.XData.IsVisible;
            xData.EffectType = eff.XData.EffectType;
        }
        return xData;
    }

    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card attackCard)
    {
        var isCastable = IsCastable();
        var validTargetForEffect = !attackCard.OnCastCardEffects.Any(effect => effect.CanNotTargetCard(this));
        if (validTargetForEffect) return false;
        if (isCastable) return false;

        canBeTargeted = false;
        return true;
    }

    public int GetCardEffectScore(ITargetable targetable)
    {
        int predictedPower = 0;

        foreach(var effect in ProjectileCardEffects)
        {
            predictedPower += effect.GetCardEffectScore(targetable);
        }

        foreach (var effect in InstantCardEffects)
        {
            predictedPower += effect.GetCardEffectScore(targetable);
        }

        foreach (var effect in OnCastCardEffects)
        {
            predictedPower += effect.GetCardEffectScore(targetable);
        }

        return predictedPower;
    }
}
