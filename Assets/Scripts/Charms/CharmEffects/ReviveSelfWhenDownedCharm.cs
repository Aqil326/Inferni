public class ReviveSelfWhenDownedCharm : Charm
{
    public ReviveSelfWhenDownedCharm(ReviveSelfWhenDownedCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.State.OnValueChanged += OnCharacterStateChange;
    }

    private void OnCharacterStateChange(CharacterState oldState, CharacterState newState)
    {
        if (oldState != CharacterState.Alive || newState != CharacterState.Downed) return;
        
        var data = GetData<ReviveSelfWhenDownedCharmData>();
        character.ReviveWithCharm(data.health, 1f, CharmData);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.State.OnValueChanged -= OnCharacterStateChange;    
        }
    }
}
