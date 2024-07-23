public class ChangeCardCastTimeCharm : Charm
{
    public ChangeCardCastTimeCharm(ChangeCardCastTimeCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        DecreaseCastingTime();
    }

    private void DecreaseCastingTime()
    {
        var data = GetData<ChangeCardCastTimeCharmData>();
        character.CastingTimeChange.Value -= data.CastTimeReduction;
    }
    
    private void IncreaseCastingTime()
    {
        var data = GetData<ChangeCardCastTimeCharmData>();
        character.CastingTimeChange.Value += data.CastTimeReduction;
    }
    
    public override void RemoveCharm()
    {
        base.RemoveCharm();
        IncreaseCastingTime();
    }
}