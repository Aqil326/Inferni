using UnityEngine;

public class SetHealthEffect : CardEffect
{
    private bool xDataSet = false;

    public SetHealthEffect(SetHealthEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<SetHealthEffectData>();
            if (character.Health.Value > data.health)
            {
                character.DealDamage(character.Health.Value - data.health, card);
            }
            else if (character.Health.Value < data.health)
            {
                character.Heal(data.health - character.Health.Value);
            }
        }
    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        var data = GetData<SetHealthEffectData>();
        number = data.health;
        effectType = CardEffectCategory.Other;
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        var data = GetData<SetHealthEffectData>();
        switch (type)
        {
            case CardXPreviewType.Initial:
            case CardXPreviewType.TrySetTarget:
                xData.EffectType = CardEffectCategory.Other;
                xData.IsVisible = true;
                xData.Value = data.health;
                break;
            case CardXPreviewType.Flying:
                if (!xDataSet)
                {
                    xData.Value = data.health; ;
                    xData.IsVisible = true;
                    xDataSet = true;
                }
                break;
            default:
                return;
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<SetHealthEffectData>();
            return data.health - character.Health.Value;
            
        }
        return base.GetCardEffectScore(target);
    }
}
