public class AddExtraEffectToCardModifierEffect : AddCharacterModifierEffect
{
    public AddExtraEffectToCardModifierEffect(AddExtraEffectToCardModifierData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<AddExtraEffectToCardModifierData>();
        string text = base.GetEffectCardText();
        var effect = data.extraEffectData.CreateEffect();
        string extraText = "";
        if(data.isProjectileEffect)
        {
            extraText = Card.PROJECTILE_EFFECT_PREFIX;
        }
        else if(data.isInstantEffect)
        {
            extraText = Card.INSTANT_EFFECT_PREFIX;
        }
        extraText += effect.GetEffectCardText();
        return text.Replace("{X}", extraText);
    }

    protected override bool ShouldApplyExtraProjectileEffects(ITargetable target, ref int damage)
    {
        return false;
    }
}

