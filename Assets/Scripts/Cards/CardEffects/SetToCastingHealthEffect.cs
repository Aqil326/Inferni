using UnityEngine;

public class SetToCastingHealthEffect : CardEffect
{
    private bool xDataSet;

    public SetToCastingHealthEffect(SetToCastingHealthEffectData data) : base(data)
    {

    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        if (!projectile) return false;

        if (projectile.Target is not Character target) return false;

        number = XData.Value;
        effectType = CardEffectCategory.Other;
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        xData.EffectType = CardEffectCategory.Other;
        switch (type)
        {
            case CardXPreviewType.Initial:
            case CardXPreviewType.TrySetTarget:
                
                xData.IsVisible = true;
                xData.Value = effectCard.Owner.Health.Value;
                break;
            case CardXPreviewType.Flying:
                if (!xDataSet)
                {
                    xData.Value = projectile.OwnerHealthWhenCast.Value;
                    xData.IsVisible = true;
                    xDataSet = true;
                }
            break;
            default:
                return;
        }
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character character || character == cardOwner) return;
        
        var health = XData.Value;
        var targetPlayer = character;
        if (targetPlayer.Health.Value > health)
        {
            var damage = targetPlayer.Health.Value - health;
            targetPlayer.DealDamage(damage, card);
        }
        else if (targetPlayer.Health.Value < health)
        {
            var heal = health - targetPlayer.Health.Value;
            targetPlayer.Heal(heal);
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is Character character)
        {
            
            if (target is not Character targetCharacter) return base.GetCardEffectScore(target);
            var xData = new XData();
            UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, targetCharacter);
            var health = XData.Value;
            return health - character.Health.Value;

        }
        return base.GetCardEffectScore(target);
    }
}
