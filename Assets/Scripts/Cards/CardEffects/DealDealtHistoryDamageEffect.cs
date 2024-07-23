using System;

public class DealDealtHistoryDamageEffect : CardEffect
{
    public DealDealtHistoryDamageEffect(DealDealtHistoryDamageEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        return base.GetEffectCardText().Replace("{X}", GetData<DealDealtHistoryDamageEffectData>().time.ToString());
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
        var effectData = GetData<DealDealtHistoryDamageEffectData>();
        var time = effectData.time;
        
        switch (type)
        {
            case CardXPreviewType.TrySetTarget:
                if (target)
                {
                    xData.Value = target.GetDealtDamageWithinTimeRange(DateTime.Now, time);
                    xData.IsVisible = true;
                    xData.EffectType = CardEffectCategory.Damage;
                }
                break;
            case CardXPreviewType.Flying:
                if (!xData.IsVisible && target)
                {
                    xData.IsVisible = true;
                    xData.Value = target.GetDealtDamageWithinTimeRange(DateTime.Now, time);
                }
                xData.EffectType = CardEffectCategory.Damage;
                break;
            case CardXPreviewType.Initial:
                xData = new XData();
                break;
            default:
                return;
        }
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character targetCharacter) return;
        
        var damage = XData.Value;
        targetCharacter.DealDamage(damage, card);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character targetCharacter) return base.GetCardEffectScore(targetable);
        var xData = new XData();
        UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, targetCharacter);
        return -xData.Value;
    }
}
        
