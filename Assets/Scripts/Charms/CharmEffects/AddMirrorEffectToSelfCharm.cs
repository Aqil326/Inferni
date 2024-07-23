
public class AddMirrorEffectToSelfCharm : Charm
{
    public AddMirrorEffectToSelfCharm(AddMirrorEffectToSelfCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.Health.OnValueChanged += OnHealthChange;
    }

    private void OnHealthChange(int oldHealth, int newHealth)
    {
        var data = GetData<AddMirrorEffectToSelfCharmData>();
        var damageAmount = data.DamageAmount;
        if (oldHealth - newHealth >= damageAmount)
        {
            AddEffectToSelf();
        }
    }
    private void AddEffectToSelf()
    {
        var data = GetData<AddMirrorEffectToSelfCharmData>();

        if (data.ModifierEffect.GetModifier() is not MirrorModifier modifier) return;
        
        var effect = data.ModifierEffect.CreateEffect();
        effect.SetOwner(character);
        character.AddModifier(modifier);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.Health.OnValueChanged -= OnHealthChange;    
        }
    }
}
