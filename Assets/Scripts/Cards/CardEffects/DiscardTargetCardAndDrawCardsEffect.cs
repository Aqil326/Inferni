using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardTargetCardAndDrawCardsEffect : CardEffect
{
    public DiscardTargetCardAndDrawCardsEffect(DiscardTargetCardAndDrawCardsEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Card targetCard) return;

        var character = cardOwner;
        var data = GetData<DiscardTargetCardAndDrawCardsEffectData>();
        float slowdown = data.Slowdown;

        character.DiscardCard(targetCard.HandIndex, slowdown);
        character.DrawCardsWithWait(data.cardDrawAmount, 1f);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        var data = GetData<DiscardTargetCardAndDrawCardsEffectData>();
        return (data.cardDrawAmount - 1) * GlobalGameSettings.Settings.CardDrawOrDiscardScore;
    }
}
