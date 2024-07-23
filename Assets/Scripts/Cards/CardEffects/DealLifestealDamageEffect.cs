using System.Collections.Generic;
using System.Data;
using UnityEngine.TextCore.Text;

public class DealLifestealDamageEffect : CardEffect
{
    private bool xDataSet = false;
    private int preEffectsCharacterHealth;

    public DealLifestealDamageEffect(DealLifestealDamageEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<DealLifestealDamageEffectData>();
        var number = data.damageAmount;
        
        if (cardOwner != null)
        {
            number = cardOwner.GetDamageToDeal(number);
        }
        
        return base.GetEffectCardText().Replace("{X}", number.ToString());
    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        effectType = ExtractEffectType(number,CardEffectCategory.Damage);
        number = XData.Value;
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        var effectData = GetData<DealLifestealDamageEffectData>();
        xData.IsVisible = true;
        xData.EffectType = CardEffectCategory.Damage;

        switch (type)
        {
            case CardXPreviewType.TrySetTarget:
                if (cardOwner != null)
                {
                    xData.Value = cardOwner.GetDamageToDeal(effectData.damageAmount);
                }
                else
                {
                    xData.Value = effectData.damageAmount;
                }
                break;
            case CardXPreviewType.Flying:
                if (!xDataSet)
                {
                    xData.Value = cardOwner.GetDamageToDeal(effectData.damageAmount);
                    xDataSet = true;
                }
                break;
            case CardXPreviewType.Initial:
                xData.IsVisible = false;
                break;

        }
    }

    public override void PreApplyEffect(List<ITargetable> targets)
    {
        base.PreApplyEffect(targets);

        foreach (var t in targets)
        {
            if (t is Character character)
            {
                preEffectsCharacterHealth += character.Health.Value;
            }
        }
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<DealLifestealDamageEffectData>();
            int totalDamage = data.damageAmount;
            if(projectile)
            {
                totalDamage = XData.Value;
            }
            character.DealDamage(totalDamage, card);
        }
    }

    public override void PostApplyEffect(List<ITargetable> targets)
    {
        base.PostApplyEffect(targets);

        int postCharacterHealth = 0;
        foreach (var t in targets)
        {
            if (t is Character character)
            {
                postCharacterHealth += character.Health.Value;
            }
        }

        int totalDamageDealt = preEffectsCharacterHealth - postCharacterHealth;
        cardOwner.Heal(totalDamageDealt);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        int totalDamage = GetData<DealLifestealDamageEffectData>().damageAmount;

        if(cardOwner != null)
        {
            totalDamage = cardOwner.GetDamageToDeal(totalDamage);
        }

        if (targetable is Character character)
        {
            var xData = new XData();
            UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, character);
            totalDamage = xData.Value;
        }
        return -totalDamage;
    }
}

