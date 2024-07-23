public class GainLifeAfterTakeDamageCharm : Charm
{
    public GainLifeAfterTakeDamageCharm(GainLifeAfterTakeDamageCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.Health.OnValueChanged += OnCharacterHealthChange;
    }

    private void OnCharacterHealthChange(int oldHealth, int newHealth)
    {
        if (oldHealth <= newHealth) return;
        
        var data = GetData<GainLifeAfterTakeDamageCharmData>();
        character.DelayHeal(data.HealAmount, data.HealDelay);
        character.RemoveCharm(CharmData);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.Health.OnValueChanged -= OnCharacterHealthChange;
        }
    }
}
