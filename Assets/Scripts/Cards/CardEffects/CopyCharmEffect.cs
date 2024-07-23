
public class CopyCharmEffect : CardEffect
{
    public CopyCharmEffect(CopyCharmEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not CharmSlot charmSlot)
        {
            return;
        }
        var targetSlotIndex = -1;
        for (var i = 0; i < cardOwner.CharmSlots.Length; i++)
        {
            if (cardOwner.CharmSlots[i].IsEmpty)
            {
                targetSlotIndex = i;
                break;
            }
        }
        if (targetSlotIndex < 0)
        {
            targetSlotIndex = cardOwner.CharmSlots.Length - 1;
        }
        
        var charmToCopy = charmSlot.Charm.CharmData;
        cardOwner.AddCharm(charmToCopy, targetSlotIndex);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if(targetable is CharmSlot slot && !slot.IsEmpty)
        {
            return slot.Charm.CharmData.score;
        }
        return base.GetCardEffectScore(targetable);
    }
}
