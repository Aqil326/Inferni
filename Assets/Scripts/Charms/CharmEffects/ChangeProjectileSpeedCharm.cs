public class ChangeProjectileSpeedCharm: Charm
{
    public ChangeProjectileSpeedCharm(ChangeProjectileSpeedCharmData charmData) : base(charmData)
    {
    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.ProjectileFiredEvent_Server += ChangeProjectileSpeed;
    }

    private void ChangeProjectileSpeed(Projectile projectile)
    {
        var data = GetData<ChangeProjectileSpeedCharmData>();
        if (data.ProjectileSpeedMultiplier > 0)
        {
            projectile.AddSpeedMultiplier(data.ProjectileSpeedMultiplier);
        }
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.ProjectileFiredEvent_Server -= ChangeProjectileSpeed;
        }
    }
}