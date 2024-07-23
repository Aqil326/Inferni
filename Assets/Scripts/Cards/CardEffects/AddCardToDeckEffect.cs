using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddCardToDeckEffect : CardEffect
{
    public AddCardToDeckEffect(AddCardToDeckEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        if (projectile == null ||
            projectile.Target is not Projectile targetProjectile ||
            targetProjectile.Owner.HandSize < targetProjectile.Owner.Hand.Length)
        {
            return string.Empty;
        }

        var data = GetData<AddCardToDeckEffectData>();
        var cardName = targetProjectile.Card.Data.Name;

        return base.GetEffectCardText().Replace("{X}", cardName);
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Projectile projectile) return;
        
        var targetCardOwner = projectile.Owner;
        if (targetCardOwner.HandSize < targetCardOwner.Hand.Length) return;
        
        projectile.DestroyProjectile();
        var card = targetCardOwner.RemoveCardFromDiscardPile(projectile.Card);
        var cardCopy = new Card(projectile.Card.Data);
        cardCopy.SetOwner(targetCardOwner);
        targetCardOwner.AddCardToTopDeckWithPosition(cardCopy, projectile.transform.position);
    }
}
