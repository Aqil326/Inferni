using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiscardPileUI : TargetableView
{
    [SerializeField]
    private TextMeshProUGUI discardAmountText;

    [SerializeField]
    private Image outlineIcon;

    [SerializeField]
    private Color selectedColor;

    private Character character;
    private Color startingColor;

    public void Init(Character character)
    {
        this.character = character;
        character.DiscardPileSize.OnValueChanged += UpdateDiscardPileNumber;
        UpdateDiscardPileNumber(0, character.DiscardPileSize.Value);
        outlineIcon.gameObject.SetActive(false);
        startingColor = outlineIcon.color;
    }

    public override bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        canBeTargeted = true;
        return true;
    }

    public override void TargetDeselected()
    {
        outlineIcon.color = startingColor;
    }

    public override void TargetSelected(bool isValidTarget)
    {
        outlineIcon.color = selectedColor;
    }

    private void UpdateDiscardPileNumber(int previous, int discardPileSize)
    {
        discardAmountText.text = discardPileSize.ToString();
    }

    public void OnPointerEnter()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
    }

    public void OnPointerExit()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(character != null)
        {
            character.DiscardPileSize.OnValueChanged -= UpdateDiscardPileNumber;
        }
    }

    protected override void OnCardDragStarted(Card card)
    {
        outlineIcon.gameObject.SetActive(true);
    }

    protected override void OnCardDragEnded(Card card)
    {
        outlineIcon.gameObject.SetActive(false);
    }

}
