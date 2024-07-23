public class CounterModifierEffect : CardEffect
{
    public CounterModifierEffect(CounterModifierEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character && character.Modifier != null)
        {
            character.Modifier.RemoveModifier();
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if(target is Character character && character.Modifier != null)
        {
            int modifierScore = character.Modifier.Data.modifierScore;
            if(character.TeamIndex == cardOwner.TeamIndex)
            {
                modifierScore *= -1;
            }
            return modifierScore;
        }
        return base.GetCardEffectScore(target);
    }
}

