
public class DestroyCharmEffect : CardEffect
{
    public DestroyCharmEffect(DestroyCharmEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not CharmSlot charmSlot)
        {
            return;
        }

        charmSlot.RemoveCharm();
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if(targetable is not CharmSlot charmSlot || charmSlot.IsEmpty)
        {
            return base.GetCardEffectScore(targetable);
        }

        return -charmSlot.Charm.CharmData.score;
    }
}
