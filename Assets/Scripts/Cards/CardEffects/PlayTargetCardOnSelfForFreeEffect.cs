using System;
using UnityEngine;

public class PlayTargetCardOnSelfForFreeEffect : CardEffect
{
    public PlayTargetCardOnSelfForFreeEffect(PlayTargetCardOnSelfForFreeEffectData data) : base(data)
    {

    }

    public override bool CanNotTargetCard(Card card)
    {
        return card.Data.Target != CardTarget.AnyPlayer;
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Card targetCard) return;

        cardOwner.TryPlayCardFromServer(targetCard.HandIndex, cardOwner);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Card targetCard) return base.GetCardEffectScore(targetable);
        return targetCard.GetCardEffectScore(cardOwner);
    }
}
