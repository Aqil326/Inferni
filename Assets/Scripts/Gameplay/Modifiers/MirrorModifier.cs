public class
    MirrorModifier : OnProjectileHitModifier
{
    public MirrorModifier(string id,
                            ModifierData modifierData,
                            float duration,
                            bool useProjectileLifetimeMultiplier) :
                            base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
    }

    public override void ApplyVisualEffects(CharacterView characterView, bool pause)
    {
        base.ApplyVisualEffects(characterView, pause);

        if (characterView != null)
        {
            SetMirrorUIVisibility(characterView);
        }
    }

    public override bool OnProjectileHit(Projectile projectile, Character character)
    {
        var currentCharacterTarget = projectile.Target is Character target ? target : null;
        var currentCharmSlotTarget = projectile.Target is CharmSlot charmSlot ? charmSlot : null;
        
        if (currentCharacterTarget == null && currentCharmSlotTarget?.Owner == null) return true;
        if (currentCharacterTarget == projectile.Owner || currentCharmSlotTarget?.Owner == projectile.Owner) return false;
        
        Character newOwner = null;
        ITargetable newTarget = null;
        if (currentCharacterTarget != null)
        {
            newOwner = currentCharacterTarget;
            newTarget = projectile.Owner;
        } else if (currentCharmSlotTarget?.Owner != null)
        {
            newOwner = currentCharmSlotTarget.Owner;
            newTarget = projectile.Owner.CharmSlots[currentCharmSlotTarget.Index];
        }

        if (newOwner == null || newTarget == null) return true;
        
        projectile.ChangeTarget(newOwner, newTarget);    
        projectile.MoveProjectile();
        return false;

    }

    private void SetMirrorUIVisibility(CharacterView characterView)
    {
        var modelObject = characterView.CharacterModel;
        var modifierEffect = characterView.ModifierEffectUI;
        
        var mirrorManager = modifierEffect.GetComponent<MirrorSpinnerManager>();
        if (mirrorManager != null)
        {
            mirrorManager.ShowMirrors(characterView, modelObject.transform);    
        }
    }
}
