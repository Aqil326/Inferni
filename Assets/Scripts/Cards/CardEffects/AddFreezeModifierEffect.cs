using UnityEngine;

public class AddFreezeModifierEffect : AddCharacterModifierEffect
{
    public AddFreezeModifierEffect(AddFreezeModifierEffectData data) : base(data)
    {

    }

    protected override void UpdateXData(Card effectCard, CardXPreviewType type, ref XData xData, Character target = null)
    {
        var modifierData = GetData<AddCharacterModifierEffectData>();
        if (!modifierData.useProjectileLifetimeMultiplier) return;

        xData.IsVisible = false;
        switch (type)
        {
            case CardXPreviewType.TrySetTarget:
                if (target == null) return;
                if (target == effectCard.Owner)
                {
                    xData.Value = 0;
                }
                else
                {
                    var distance = Vector3.Distance(target.transform.position, effectCard.Owner.transform.position);
                    var estimatedLifetime =  Mathf.RoundToInt(distance / effectCard.ProjectileSpeed) - 1;
                    var modifier = modifierData.GetModifier();
                    xData.Value = Mathf.RoundToInt(estimatedLifetime * modifier.Duration);    
                }
                xData.EffectType = ExtractEffectType(xData.Value, CardEffectCategory.Other);
                xData.IsVisible = true;
                break;
            case CardXPreviewType.Flying:
                xData.IsVisible = true;
                xData.EffectType = CardEffectCategory.Other;
                if (projectile)
                {
                    var timeSpan = projectile.LifeTime.Value - projectile.LastXCalculationTime.Value;
                    if (timeSpan >= 1)
                    {
                        xData.Value += Mathf.RoundToInt(modifierData.duration * timeSpan);
                        projectile.LastXCalculationTime.Value = projectile.LifeTime.Value;
                    }
                }
                break;
            case CardXPreviewType.Initial:
            default:
                return;
        }
    }
    
    protected override void InternalApplyEffect(ITargetable target)
    {
        var data = GetData<AddCharacterModifierEffectData>();
        var modifier = data.GetModifier() as CharacterModifier;

        if(data.useProjectileLifetimeMultiplier)
        {
            modifier.Duration = XData.Value;
            if (modifier.Duration == 0) return;
        }
        if (target is Character character)
        {
            character.AddModifier(modifier);
        }
        
    }
}

