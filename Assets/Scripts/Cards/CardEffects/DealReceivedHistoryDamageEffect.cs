using System;

public class DealReceivedHistoryDamageEffect : CardEffect
{
    public DealReceivedHistoryDamageEffect(DealReceivedHistoryDamageEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        return base.GetEffectCardText().Replace("{X}", GetData<DealReceivedHistoryDamageEffectData>().time.ToString());
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
        var effectData = GetData<DealReceivedHistoryDamageEffectData>();
        var time = effectData.time;
        switch (type)
        {
            case CardXPreviewType.Initial:
            case CardXPreviewType.TrySetTarget:
                xData.Value = effectCard.Owner.GetReceivedDamageWithinTimeRange(DateTime.Now, time);
                xData.EffectType = CardEffectCategory.Damage;
                xData.IsVisible = true;
                break;
            case CardXPreviewType.Flying:
                if (!xData.IsVisible && projectile)
                {
                    xData.IsVisible = true;
                    xData.Value = projectile.Card.Owner.GetReceivedDamageWithinTimeRange(DateTime.Now, time);
                }
                xData.EffectType = CardEffectCategory.Damage;
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
        if (targetable is not Character targetCharacter)
        {
            return base.GetCardEffectScore(targetable);
        }
        var xData = new XData();
        UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, targetCharacter);
        return -xData.Value;
    }
}
        
