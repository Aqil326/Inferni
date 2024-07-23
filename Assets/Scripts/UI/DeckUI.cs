using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI deckAmountText;

    [SerializeField]
    private Image timerImage;

    private Character character;
    private Coroutine drawTimerCoroutine;

    public void Init(Character character)
    {
        this.character = character;
        character.DeckSize.OnValueChanged += UpdateDeckAmountText;
        character.RemainingDrawTime.OnValueChanged += UpdateCardDrawTimer;
        UpdateDeckAmountText(0, character.DeckSize.Value);
    }


    private void UpdateDeckAmountText(int previous, int deckAmount)
    {
        deckAmountText.text = "x" + $"{deckAmount}";
    }    

    private void UpdateCardDrawTimer(float previous, float current)
    {
        timerImage.fillAmount = current / character.Data.CardDrawTimer;
    }

    private void OnDestroy()
    {
        if(character != null)
        {
            character.DeckSize.OnValueChanged -= UpdateDeckAmountText;
            character.RemainingDrawTime.OnValueChanged -= UpdateCardDrawTimer;
        }
    }
}
