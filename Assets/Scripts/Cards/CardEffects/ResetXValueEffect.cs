using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetXValueEffect : CardEffect
{
    public ResetXValueEffect(ResetXValueEffectData data) : base(data)
    {
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Projectile projectile) return;
        foreach (var projectileEff in projectile.Card.ProjectileCardEffects)
        {
            projectileEff.XData.Value = 0;
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is not Projectile projectile) return base.GetCardEffectScore(target);

        if (projectile.Card.XData.Value == 0) return GlobalGameSettings.Settings.BotSettings.NonXDataCardScore;

        int cardScore = -projectile.Card.GetCardEffectScore(projectile.Target);
        if(projectile.Target is Character character && character.TeamIndex == cardOwner.TeamIndex)
        {
            cardScore *= -1;
        }
        return cardScore;
    }
}
