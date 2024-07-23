using UnityEngine;

public class DrawCardsWhenHealthChangedCharm : Charm
{
    public DrawCardsWhenHealthChangedCharm(DrawCardsWhenHealthChangedCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.Health.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        var data = GetData<DrawCardsWhenHealthChangedCharmData>();
        int difference = oldHealth - newHealth;
        if(data.healthChange > 0 && difference >= data.healthChange)
        {
            DrawCards();
        }
        else if(data.healthChange < 0 && difference <= data.healthChange)
        {
            DrawCards();
        }
    }

    private void DrawCards()
    {
        var data = GetData<DrawCardsWhenHealthChangedCharmData>();
        for(int i = 0; i < data.cardsDrawn; i++)
        {
            character.DrawCard();
        }
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.Health.OnValueChanged -= OnHealthChanged;
        }
    }
}
