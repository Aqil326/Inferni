public class AddGlobalModifierEffect : CardEffect
{
    public AddGlobalModifierEffect(AddGlobalModifierEffectData data) : base(data)
    {
    }

    public override string GetEffectCardText()
    {
        var modifierData = GetData<AddGlobalModifierEffectData>();
        string effectText = base.GetEffectCardText().Replace("{X}", modifierData.modifierData.modifierName)
            .Replace("{Y}", modifierData.duration.ToString());
        return effectText;
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        var data = GetData<AddGlobalModifierEffectData>();
        var modifier = data.GetModifier() as GlobalModifier;

        if (target is World world)
        {
            world.AddModifier(modifier);
        }
    }

    public override bool HasKeyword(out TooltipData keywordData)
    {
        var modifierData = GetData<AddGlobalModifierEffectData>();
        keywordData = new TooltipData()
        {
            Title = modifierData.modifierData.modifierName,
            Description = modifierData.modifierData.description,
            IconImage = modifierData.modifierData.modifierIcon
        };
        return true;
    }
}

