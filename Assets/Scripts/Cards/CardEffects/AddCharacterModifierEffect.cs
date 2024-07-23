
public class AddCharacterModifierEffect : CardEffect
{ 
    public AddCharacterModifierEffect(AddCharacterModifierEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var modifierData = GetData<AddCharacterModifierEffectData>();
        string effectText = base.GetEffectCardText().Replace("{X}", modifierData.modifierData.modifierName)
            .Replace("{Y}", modifierData.duration.ToString());

        return effectText;
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        var data = GetData<AddCharacterModifierEffectData>();
        var modifier = data.GetModifier() as CharacterModifier;

        if (projectile != null && data.useProjectileLifetimeMultiplier)
        {
            modifier.Duration *= projectile.LifeTime.Value;
        }

        if (target is Character character)
        {
            character.AddModifier(modifier);
        }
        else if(target is Projectile projectile)
        {
            projectile.Owner.AddModifier(modifier);
        }
    }

    public override bool HasKeyword(out TooltipData keywordData)
    {
        var modifierData = GetData<AddCharacterModifierEffectData>();
        keywordData = new TooltipData()
        {
            Title = modifierData.modifierData.modifierName,
            Description = modifierData.modifierData.description,
            IconImage = modifierData.modifierData.modifierIcon
        };
        return true;
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        var modifier = GetData<AddCharacterModifierEffectData>().GetModifier() as CharacterModifier;
        return modifier.Data.modifierScore;
    }
}

