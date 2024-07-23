public class AddExtraEffectToProjectilesCharm : Charm
{
    public AddExtraEffectToProjectilesCharm(AddExtraEffectToProjectilesCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.CardPlayedEvent_Server += AddEffectToCard;
    }

    private void AddEffectToCard(Card card)
    {
        var data = GetData<AddExtraEffectToProjectilesCharmData>();

        bool matchEffect = false;
        foreach (var eff in card.ProjectileCardEffects)
        {
            foreach (var category in eff.Data.CardEffectCategories)
            {
                if (category == data.requestedEffectType)
                {
                    matchEffect = true;
                    break;
                }
            }
        }

        if(!matchEffect)
        {
            return;
        }

        var extraEffect = data.cardEffectData.CreateEffect();
        extraEffect.SetOwner(character);
        extraEffect.SetProjectile(card.Projectile);
        card.ProjectileCardEffects.Add(extraEffect);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.CardPlayedEvent_Server -= AddEffectToCard;    
        }
    }
}
