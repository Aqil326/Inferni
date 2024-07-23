using UnityEngine.TextCore.Text;

public class CounterCardEffect : CardEffect
{
    public CounterCardEffect(CounterCardEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            character.CancelCasting();
        }
        else if(target is Projectile projectile)
        {
            projectile.DestroyProjectile();
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is Projectile projectile)
        {
            int projectileScore = projectile.Card.GetCardEffectScore(projectile.Target);
            if (projectile.Target is Character character && character.TeamIndex != cardOwner.TeamIndex)
            {
                projectileScore *= -1;
            }
            return projectileScore;
        }
        return base.GetCardEffectScore(target);
    }
}

