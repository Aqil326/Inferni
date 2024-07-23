public class DrawCardAfterKillPlayerCharm : Charm
{
    public DrawCardAfterKillPlayerCharm(DrawCardAfterKillPlayerCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        characterManager.CharacterStateChangedEvent += OnCharacterStateChanged;
    }
    
    public override void RemoveCharm()
    {
        base.RemoveCharm();
        characterManager.CharacterStateChangedEvent -= OnCharacterStateChanged;
    }

    private void OnCharacterStateChanged(Character character)
    {
        if (character == null || character.State.Value != CharacterState.Dead) return;

        character.DrawCard();    
    }
}
