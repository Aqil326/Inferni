using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplyXValueEffect : CardEffect
{
    public MultiplyXValueEffect(MultiplyXValueEffectData data) : base(data)
    {
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Projectile projectile) return;
        foreach(var projectileEff in projectile.Card.ProjectileCardEffects)
        {
            projectileEff.XData.Value = Mathf.FloorToInt(projectileEff.XData.Value * GetData<MultiplyXValueEffectData>().multiplier);
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is not Projectile projectile) return base.GetCardEffectScore(target);

        if (projectile.Card.XData.Value == 0) return GlobalGameSettings.Settings.BotSettings.NonXDataCardScore;

        int cardScore = (int)(projectile.Card.GetCardEffectScore(projectile.Target) * GetData<MultiplyXValueEffectData>().multiplier);
        return cardScore;
    }
}
