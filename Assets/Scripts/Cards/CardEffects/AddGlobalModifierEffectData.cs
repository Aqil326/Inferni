public abstract class AddGlobalModifierEffectData : AddModifierEffectData
{
    public override CardEffect CreateEffect()
    {
        return new AddGlobalModifierEffect(this);
    }

}


