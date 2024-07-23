public class InstaKillEffect : CardEffect
{
    public InstaKillEffect(InstaKillEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if(target is Character character)
        {
            character.Kill(card);
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character character) return base.GetCardEffectScore(targetable);
        return -character.Health.Value;
    }
}
