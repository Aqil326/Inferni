public abstract class AddCharacterModifierEffectData : AddModifierEffectData
{
    public override CardEffect CreateEffect()
    {
        return new AddCharacterModifierEffect(this);
    }
}


