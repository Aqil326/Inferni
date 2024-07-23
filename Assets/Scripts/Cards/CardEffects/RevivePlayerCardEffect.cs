public class RevivePlayerCardEffect : CardEffect
{
    public RevivePlayerCardEffect(RevivePlayerCardEffectData data) : base(data)
    {

    }
    public override string GetEffectCardText()
    {
        var data = GetData<RevivePlayerCardEffectData>();
        var text = base.GetEffectCardText();
        text = text.Replace("{X}", data.Health.ToString());
        return text;
    }
    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character character) return;
        
        card.Owner.RemoveCardFromDiscardPile(projectile.Card);
        var data = GetData<RevivePlayerCardEffectData>();
        character.Revive(data.Health);
    }
}

