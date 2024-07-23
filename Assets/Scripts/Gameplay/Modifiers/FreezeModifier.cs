using UnityEngine;
using UnityEngine.TextCore.Text;

public class FreezeModifier : CharacterModifier
{
    public FreezeModifier(string id,
                            ModifierData modifierData,
                            float duration,
                            bool useProjectileLifetimeMultiplier) :
                            base(id, modifierData, duration, useProjectileLifetimeMultiplier)
    {
    }
    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.SetDisablePlayCardsStatus(true);
    }

    public override void ApplyVisualEffects(CharacterView characterView, bool pause)
    {
        base.ApplyVisualEffects(characterView, pause);

        if (characterView != null)
        {
            SetFrozenUI(true, characterView.transform);

        }
    }

    public override void RemoveVisualEffects(CharacterView characterView, bool pause)
    {
        base.RemoveVisualEffects(characterView, pause);

        if (characterView != null)
        {
            SetFrozenUI(false, characterView.transform);
        }
    }

    public override void RemoveModifier()
    {
        base.RemoveModifier();
        Character.SetDisablePlayCardsStatus(false);
    }
    
    private void SetFrozenUI(bool isFrozen, Transform parentTransform)
    {
        var modelObject = parentTransform.GetComponentInChildren<CharacterModel>();
        var frozenBoxUI = parentTransform.GetComponentInChildren<FrozenBox>();
        
        if (isFrozen)
        {
            //var modelTransform = modelObject.transform;
            //var frozenBoxTransform = frozenBoxUI.transform;
            
            //frozenBoxTransform.position = modelTransform.position;
            //var localScale = modelTransform.localScale;
            //frozenBoxTransform.localScale = new Vector3(localScale.x * 1.3f, localScale.y * 4, localScale.z);
            //frozenBoxTransform.rotation = modelTransform.rotation;
            frozenBoxUI.gameObject.SetActive(true);
        }
        else
        {
            frozenBoxUI.gameObject.SetActive(false);
        }
    }
}
