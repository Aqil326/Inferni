using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnSpellToHandEffect : CardEffect
{
    public ReturnSpellToHandEffect(ReturnSpellToHandEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Projectile projectile) return;
        
        var targetCardOwner = projectile.Card.OriginalOwner;
        if (targetCardOwner.HandSize == targetCardOwner.Hand.Length) return;
        
        projectile.DestroyProjectile();
        targetCardOwner.RemoveCardFromDiscardPile(projectile.Card);
        var cardCopy = new Card(projectile.Card.Data);
        cardCopy.SetOwner(targetCardOwner);
        targetCardOwner.AddCardToHandWithPosition(cardCopy, projectile.transform.position);
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        if (target is not Projectile projectile) return base.GetCardEffectScore(target);
        return -projectile.Card.GetCardEffectScore(projectile.Target);
    }
}
