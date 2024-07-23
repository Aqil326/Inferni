using System;
using System.Collections.Generic;

public abstract class CardEffect
{
    public CardEffectData Data { get; private set; }

    public virtual bool HasTimeScaling => false;

    protected Character cardOwner;
    protected Projectile projectile;
    protected Card card;
    
    protected CharacterManager characterManager;

    public XData XData;

    public CardEffect(CardEffectData data)
    {
        this.Data = data;
        characterManager = GameManager.GetManager<CharacterManager>();
    }

    public void Init(Card card)
    {
        this.card = card;
    }

    public void SetOwner(Character cardOwner)
    {
        this.cardOwner = cardOwner;
    }

    protected T GetData<T>() where T:CardEffectData
    {
        return Data as T;
    }

    public virtual bool HasKeyword(out TooltipData keywordData)
    {
        keywordData = new TooltipData();
        return false;
    }

    public bool IsProjectileNumberVisible(ref int number, ref CardEffectCategory effectType)
    {
        var hasProjectileNumber = HasProjectileNumber(ref number, ref effectType);
        return hasProjectileNumber;
    }
    
    protected virtual bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        return false;
    }
    
    public virtual bool CanNotTargetCard(Card card)
    {
        return false;
    }
    
    public virtual string GetEffectCardText()
    {
        return Data.effectDescription;
    }

    protected virtual void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {

    }

    public void UpdateXData(Card effectCard, CardXPreviewType type, Character target = null)
    {
        UpdateXData(effectCard, type, ref XData, target);
    }

    public virtual void SetProjectile(Projectile projectile)
    {
        this.projectile = projectile;
    }

    public List<ITargetable> GetEffectTargets(List<ITargetable> cardTargets)
    {
        var effectTargets = new List<ITargetable>();

        switch (Data.target)
        {
            case CardEffectTarget.CardTargets:
                foreach (var t in cardTargets)
                {
                    effectTargets.Add(t);
                }
                break;
            case CardEffectTarget.Owner:
                effectTargets.Add(cardOwner);
                break;
            case CardEffectTarget.All:
                foreach (var t in characterManager.Characters)
                {
                    effectTargets.Add(t);
                }
                break;
            case CardEffectTarget.AllOpponents:
                foreach (var t in characterManager.GetAllOpponents(cardOwner))
                {
                    effectTargets.Add(t);
                }
                break;
            case CardEffectTarget.AllTeammates:
                foreach (var t in characterManager.Characters)
                {
                    if (t.TeamIndex == cardOwner.TeamIndex)
                    {
                        effectTargets.Add(t);
                    }
                }
                break;
            case CardEffectTarget.AllProjectiles:
                var cardEffectsManager = GameManager.GetManager<CardsEffectsManager>();
                foreach (var p in cardEffectsManager.Projectiles)
                {
                    effectTargets.Add(p);
                }
                break;
            case CardEffectTarget.World:
                effectTargets.Add(GameManager.Instance.World);
                break;
        }
        return effectTargets;
    }

    public void ApplyEffectToTargets(List<ITargetable> cardTargets)
    {
        var effectTargets = GetEffectTargets(cardTargets);

        foreach(var t in effectTargets)
        {
            ApplyEffect(t);
        }
    }

    protected CardEffectCategory ExtractEffectType(int number, CardEffectCategory checkType)
    {
        if (number >= 0)
        {
            return checkType;
        }

        if (number < 0)
        {
            switch (checkType)
            {
                case CardEffectCategory.Damage:
                    return CardEffectCategory.Healing;
                case CardEffectCategory.Healing:
                    return CardEffectCategory.Damage;
                default:
                    return CardEffectCategory.Other;
            }
        }

        return CardEffectCategory.Other;
    }

    public virtual void PreApplyEffect(List<ITargetable> targets)
    {

    }

    public virtual void PostApplyEffect(List<ITargetable> target)
    {

    }

    public void ApplyEffect(ITargetable target, Card card = null)
    {
        if (card != null)
        {
            this.card = card;
        }
        InternalApplyEffect(target);
        ApplyExtraProjectileEffects(target);
    }

    private void ApplyExtraProjectileEffects(ITargetable target)
    {
        var damage = 0;
        if (ShouldApplyExtraProjectileEffects(target, ref damage))
        {
            ((Character)target).DealDamage(damage, card);
        }
    }

    private int GetAdditionalDamage(ITargetable target)
    {
        if (cardOwner == null || target is not Character || projectile == null) return 0;
        return cardOwner.GetAdditionalDamage();
    }

    protected virtual bool ShouldApplyExtraProjectileEffects(ITargetable target, ref int damage)
    {
        damage = GetAdditionalDamage(target); 
        return  damage > 0;
    }

    protected abstract void InternalApplyEffect(ITargetable target);

    public virtual bool HasSubCard(out CardData subCard)
    {
        subCard = null;
        return false;
    }

    public virtual int GetCardEffectScore(ITargetable targetable)
    {
        return 0;
    }
}

