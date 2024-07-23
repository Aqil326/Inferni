using UnityEngine;

public class ChangeCastingTimerEffect : CardEffect
{
    public ChangeCastingTimerEffect(ChangeCastingTimerEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<ChangeCastingTimerEffectData>();
        return base.GetEffectCardText().Replace("{X}", data.castTimerAddition.ToString()).Replace("{Y}", data.castTimerMultiplier.ToString());
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if(target is CastingTargetable castingTargetable)
        {
            if(!castingTargetable.Owner.IsCasting.Value)
            {
                Debug.LogError("Can't change casting time if Player is not casting!");
                return;
            }

            var data = GetData<ChangeCastingTimerEffectData>();

            if(data.castTimerAddition != 0)
            {
                castingTargetable.Owner.AddToCastingTime(data.castTimerAddition);
            }

            if(data.castTimerMultiplier != 1)
            {
                castingTargetable.Owner.MultiplyCastingTime(data.castTimerMultiplier);
            }
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        var data = GetData<ChangeCastingTimerEffectData>();
        return (int) -(1 - (1 - data.castTimerAddition) * data.castTimerMultiplier);
    }
}

