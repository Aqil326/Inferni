using Unity.VisualScripting;
using UnityEngine;

public class ApplySubCardEffect : CardEffect
{
    public ApplySubCardEffect(ApplySubCardEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<ApplySubCardEffectData>();
        var text = base.GetEffectCardText();
        text = text.Replace("{X}", data.cardData.Name);
        var card = new Card(data.cardData);
        text = text.Replace("{Y}", card.GetCardText());
        return text;
    }

    protected override bool ShouldApplyExtraProjectileEffects(ITargetable target, ref int damage)
    {
        return false;
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        { 
             if(character.Health.Value <= 0)
             {
                 return;
             }
        }
        
        var data = GetData<ApplySubCardEffectData>();
        cardOwner.PlayCard(data.cardData, target, true);
        
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        var cardData = GetData<ApplySubCardEffectData>().cardData;
        return new Card(cardData).GetCardEffectScore(targetable);
    }

    public override bool HasSubCard(out CardData subCard)
    {
        subCard = GetData<ApplySubCardEffectData>().cardData;
        return true;
    }
}
