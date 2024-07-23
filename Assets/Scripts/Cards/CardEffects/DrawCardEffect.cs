public class DrawCardEffect : CardEffect
{
    public DrawCardEffect(DrawCardEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        return base.GetEffectCardText().Replace("{X}", GetData<DrawCardEffectData>().cardsAmount.ToString());
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            for(int i = 0; i < GetData<DrawCardEffectData>().cardsAmount; i++)
            {
                character.DrawCard();
            }
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        return GetData<DrawCardEffectData>().cardsAmount * GlobalGameSettings.Settings.CardDrawOrDiscardScore;
    }
}
