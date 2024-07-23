public class SwitchPlayersEffect : CardEffect
{
    public SwitchPlayersEffect(SwitchPlayersEffectData data) : base (data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        //TODO: SwitchCharacters is currently not working, needs rework
        if(target is Character character)
        {
            //cardOwner.CharacterView.SetCharacter(view.Character);
            //view.SetCharacter(cardOwner);
        }
    }
}

