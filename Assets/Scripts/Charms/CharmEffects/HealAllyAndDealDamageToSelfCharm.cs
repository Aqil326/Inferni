public class HealAllyAndDealDamageToSelfCharm: Charm
{
    public HealAllyAndDealDamageToSelfCharm(HealAllyAndDealDamageToSelfCharmData charmData) : base(charmData)
    {
    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        var data = GetData<HealAllyAndDealDamageToSelfCharmData>();
        character.HealAllyDealDamageToSelf(data.HealthDiff, data.HealAmount, data.DamageAmount, data.Frequency);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.RemoveAutoHealAndDamageCoroutine();
        }
    }
}