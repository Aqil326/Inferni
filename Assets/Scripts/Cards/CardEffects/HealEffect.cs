public class HealEffect : CardEffect
{
    private bool xDataSet = false;

    public HealEffect(HealEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<HealEffectData>();
        return base.GetEffectCardText().Replace("{X}", data.healAmount.ToString());
    }

    protected override bool HasProjectileNumber(ref int number, ref CardEffectCategory effectType)
    {
        number = XData.Value;
        effectType = ExtractEffectType(number, CardEffectCategory.Healing);
        return true;
    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        var effectData = GetData<HealEffectData>();
        xData.IsVisible = true;
        xData.EffectType = CardEffectCategory.Healing;
        
        switch (type)
        {
            case CardXPreviewType.TrySetTarget:
                xData.Value = effectData.healAmount;
                break;
            case CardXPreviewType.Flying:
                if(!xDataSet)
                {
                    xData.Value = effectData.healAmount;
                    xDataSet = true;
                }
                break;
            case CardXPreviewType.Initial:
                xData.IsVisible = false;
                break;

        }
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<HealEffectData>();
            int healAmount = data.healAmount;
            if (projectile)
            {
                healAmount = XData.Value;
            }
            character.Heal(healAmount);
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character character)
        {
            return GetData<HealEffectData>().healAmount;
        }
        var xData = new XData();
        UpdateXData(card, CardXPreviewType.TrySetTarget, ref xData, character);
        return xData.Value;
        
    }
}

