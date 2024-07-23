using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private HandUI handUI;

    [SerializeField]
    private DeckUI deckUI;

    [SerializeField]
    private DiscardPileUI discardPileUI;

    [SerializeField]
    private HealthBar healthBar;

    [SerializeField]
    private EnergyPoolUI energyPoolUIPrefab;

    [SerializeField]
    private Transform energyPoolUIParent;

    [SerializeField]
    private Button cancelCastingButton;

    [SerializeField]
    private CharmSlotUI[] charmSlots;

    [SerializeField]
    private PromptController promptController;

    private Character character;

    private void Start()
    {
        canvasGroup.alpha = 0;
    }

    public void Init(Character character)
    {
        canvasGroup.alpha = 1;

        this.character = character;
        handUI.Init(character);
        deckUI.Init(character);
        discardPileUI.Init(character);
        healthBar.Init(character);

        cancelCastingButton.gameObject.SetActive(false);
        character.CardCastingStartEvent += ShowCancelCasting;
        character.CardCastingEndEvent += HideCancelCasting;

        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnDraftEnded);

        for (int i = 0; i < character.CharmSlots.Length; i++)
        {
            CharmSlot slot = character.CharmSlots[i];
            charmSlots[i].Init(slot);
        }

        promptController.Init();
    }

    private void ShowCancelCasting(int cardIndex, float timer, CastingType castingType)
    {
        cancelCastingButton.gameObject.SetActive(true);
    }

    private void HideCancelCasting()
    {
        cancelCastingButton.gameObject.SetActive(false);
    }

    public void CancelCasting()
    {
        character.CancelCastingServerRPC();
    }

    private void OnDraftStarted()
    {
        cancelCastingButton.interactable = false;
    }

    private void OnDraftEnded()
    {
        cancelCastingButton.interactable = true;
    }

    private void OnDestroy()
    {
        if(character != null)
        {
            character.CardCastingStartEvent -= ShowCancelCasting;
            character.CardCastingEndEvent -= HideCancelCasting;
        }

        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnDraftEnded);
    }

}
