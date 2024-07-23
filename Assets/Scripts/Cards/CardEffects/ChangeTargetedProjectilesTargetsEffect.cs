public class ChangeTargetedProjectilesTargetsEffect : CardEffect
{
    public ChangeTargetedProjectilesTargetsEffect(ChangeTargetedProjectilesTargetsEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var projectiles = GameManager.GetManager<CardsEffectsManager>().Projectiles;

            foreach(var p in projectiles)
            {
                if(p.Target == cardOwner && p != projectile && cardOwner != target)
                {
                    p.ChangeTarget(cardOwner, target);
                }
            }
        }
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is Character character)
        {
            int finalScore = 0;

            var projectiles = GameManager.GetManager<CardsEffectsManager>().Projectiles;

            foreach (var p in projectiles)
            {
                if (p.Target == cardOwner && cardOwner != target)
                {
                    int projectileScore = p.Card.GetCardEffectScore(cardOwner);
                    finalScore += projectileScore;
                }
            }
            return finalScore;
        }
        return base.GetCardEffectScore(target);
    }
}
