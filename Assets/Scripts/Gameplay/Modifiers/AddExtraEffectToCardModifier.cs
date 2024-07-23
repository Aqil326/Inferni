public class AddExtraEffectToCardModifier : CharacterModifier
{
    private CardEffectData extraEffectData;
    private bool isProjectileEffect;
    private bool isInstantEffect;

    public AddExtraEffectToCardModifier(CardEffectData extraEffectData,
                        bool isProjectileEffect,
                        bool isInstantEffect,
                        string id,
                        ModifierData modifierData,
                        float duration,
                        bool useProjectileLifetimeMultiplier) :
                        base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
        this.extraEffectData = extraEffectData;
        this.isInstantEffect = isInstantEffect;
        this.isProjectileEffect = isProjectileEffect;
    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.CardPlayedEvent_Server += AddEffectToCard;
    }

    public override void RemoveModifier()
    {
        base.RemoveModifier();
        Character.CardPlayedEvent_Server -= AddEffectToCard;
    }

    private void AddEffectToCard(Card card)
    {
        var extraEffect = extraEffectData.CreateEffect();
        extraEffect.SetOwner(Character);
        extraEffect.SetProjectile(card.Projectile);
        if (isProjectileEffect)
        {
            card.ProjectileCardEffects.Add(extraEffect);
        }

        if(isInstantEffect)
        {
            card.InstantCardEffects.Add(extraEffect);
        }
    }
}
