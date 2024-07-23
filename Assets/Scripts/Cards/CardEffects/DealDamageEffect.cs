using UnityEngine;

public class DealDamageEffect : CardEffect
{
    private bool xDataSet = false;

    public DealDamageEffect(DealDamageEffectData data) : base(data)
    {

    }

    public override bool HasTimeScaling => GetData<DealDamageEffectData>().useProjectileLifetimeMultiplier;

    public override string GetEffectCardText()
    {
        var data = GetData<DealDamageEffectData>();
        int number = data.damageAmount;
        string text = base.GetEffectCardText();
        if (data.useProjectileLifetimeMultiplier)
        {
            text = text.Replace("{Y}", data.DelayTime.ToString());
        }
        else if (cardOwner != null)
        {
            number = cardOwner.GetDamageToDeal(data.damageAmount);
        }
        
        return text.Replace("{X}", number.ToString());
    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        effectType = ExtractEffectType(number, CardEffectCategory.Damage);
        number = XData.Value;
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        var effectData = GetData<DealDamageEffectData>();

        xData.IsVisible = false;
        var delayTime = effectData.DelayTime;
        switch (type)
        {
            case CardXPreviewType.TrySetTarget:
                if (!target) return;
                if (effectData.useProjectileLifetimeMultiplier)
                {
                    if (target == effectCard.Owner)
                    {
                        xData.Value = 0;
                    }
                    else
                    {
                        var distance = Vector3.Distance(target.transform.position, effectCard.Owner.transform.position);
                        var estimatedProjectileLifetime = distance / effectCard.ProjectileSpeed - 1;
                        xData.Value = Mathf.RoundToInt((cardOwner.GetDamageToDeal(effectData.damageAmount) * estimatedProjectileLifetime) / delayTime);
                    }
                }
                else
                {
                    if (cardOwner == null)
                    {
                        xData.Value = effectData.damageAmount;
                    }
                    else
                    {
                        xData.Value = cardOwner.GetDamageToDeal(effectData.damageAmount);
                    }
                }
                xData.EffectType = ExtractEffectType(XData.Value, CardEffectCategory.Damage);
                xData.IsVisible = true;
                break;
            case CardXPreviewType.Flying:
                xData.IsVisible = true;
                xData.EffectType = CardEffectCategory.Damage;
                if (projectile && effectData.useProjectileLifetimeMultiplier)
                {
                    var timeSpan = (projectile.LifeTime.Value - projectile.LastXCalculationTime.Value) / delayTime;

                    if (timeSpan >= 1)
                    {
                        var damageAmount = cardOwner.GetDamageToDeal(effectData.damageAmount);
                        xData.Value += Mathf.RoundToInt(damageAmount * timeSpan);
                        projectile.LastXCalculationTime.Value = projectile.LifeTime.Value;
                    }
                    return;
                }

                if (!xDataSet)
                {
                    xData.Value = cardOwner.GetDamageToDeal(effectData.damageAmount);
                    xDataSet = true;
                }
                break;
            case CardXPreviewType.Initial:
                break;
            default:
                return;
        }
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<DealDamageEffectData>();
            var totalDamage = cardOwner.GetDamageToDeal(data.damageAmount);
            if (projectile)
            {
                totalDamage = XData.Value;
            }

            foreach(var bonus in data.characterPositionBonuses)
            {
                totalDamage = bonus.GetValue(totalDamage, character);
            }

            character.DealDamage(totalDamage, card);
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        var data = GetData<DealDamageEffectData>();

        int totalDamage = 0;
        if (targetable is Character character)
        {
            var xData = new XData();
            UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, character);
            totalDamage = xData.Value;
            foreach (var bonus in data.characterPositionBonuses)
            {
                totalDamage += bonus.GetValue(totalDamage, character);
            }
        }
        else
        {
            totalDamage = cardOwner.GetDamageToDeal(data.damageAmount);
        }
        return -totalDamage;
    }
}
