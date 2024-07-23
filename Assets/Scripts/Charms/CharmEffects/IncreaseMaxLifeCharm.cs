public class IncreaseMaxLifeCharm : Charm
{
    public IncreaseMaxLifeCharm(IncreaseMaxLifeCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        IncreaseMaxLife();
    }

    private void IncreaseMaxLife()
    {
        var data = GetData<IncreaseMaxLifeCharmData>();
        character.MaxHealth.Value += data.LifeAmount;
    }
    
    private void DecreaseMaxLife()
    {
        var data = GetData<IncreaseMaxLifeCharmData>();
        character.MaxHealth.Value -= data.LifeAmount;
        
        if (character.Health.Value > character.MaxHealth.Value)
        {
            character.Health.Value = character.MaxHealth.Value;
        }
    }
    
    public override void RemoveCharm()
    {
        base.RemoveCharm();
        DecreaseMaxLife();
    }
}
