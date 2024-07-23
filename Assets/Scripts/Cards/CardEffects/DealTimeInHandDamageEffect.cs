using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DealTimeInHandDamageEffect : CardEffect
{
    public DealTimeInHandDamageEffect(DealTimeInHandDamageEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<DealTimeInHandDamageEffectData>();
        return base.GetEffectCardText().Replace("{X}", data.DelayTime.ToString()).Replace("{Y}", data.MaxDamageValue.ToString());
    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        if (!projectile) return false;

        number = XData.Value;
        effectType = ExtractEffectType(number, CardEffectCategory.Damage);
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        xData.EffectType = CardEffectCategory.Damage;
        if (type == CardXPreviewType.Flying && xData.IsVisible) return;

        xData.IsVisible = true;
        xData.Value = GetDamageByTimeInHand(effectCard);        
    }
    
    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            int totalDamage = GetDamageByTimeInHand(card);
            if(projectile)
            {
                totalDamage = XData.Value;
            }
            character.DealDamage(totalDamage, card);
        }
    }

    private int GetDamageByTimeInHand(Card card)
    {
        var data = GetData<DealTimeInHandDamageEffectData>();
        var dataDelayTime = data.DelayTime;
        var dataMaxDamageValue = data.MaxDamageValue;
        if (dataDelayTime <= 0 || dataMaxDamageValue <= 0) return 0;
        var timeInHand = projectile == null ? card.TimeInHand : projectile.TimeInHand.Value;
        var damage = Mathf.FloorToInt(timeInHand / dataDelayTime);
        return Mathf.Min(damage, dataMaxDamageValue);

    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character targetCharacter)
        {
            return base.GetCardEffectScore(targetable);
        }

        var xData = new XData();
        UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, targetCharacter);
        return -xData.Value;
    }
}
