public class ChangeProjectileSpeedEffect : CardEffect
{
    public ChangeProjectileSpeedEffect(ChangeProjectileSpeedEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        return base.GetEffectCardText().Replace("{X}", GetData<ChangeProjectileSpeedEffectData>().speedMultiplier.ToString());
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if(target is Projectile projectile)
        {
            projectile.AddSpeedMultiplier(GetData<ChangeProjectileSpeedEffectData>().speedMultiplier);
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is Projectile projectile)
        {
            if(projectile.Target is Character character)
            {
                int baseScore = projectile.Card.GetCardEffectScore(projectile.Target);

                foreach(var e in projectile.Card.ProjectileCardEffects)
                {
                    if(e.HasTimeScaling)
                    {
                        baseScore *= -1;
                        break;
                    }
                }

                if (character.TeamIndex == cardOwner.TeamIndex)
                {
                    baseScore *= -1;
                }

                return baseScore;
            }
        }
        return base.GetCardEffectScore(target);
    }
}

