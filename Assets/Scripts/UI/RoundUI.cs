using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RoundUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI roundInfoText;
    [SerializeField]
    private TextMeshProUGUI roundTimerText;
    [SerializeField]
    private Image roundTimerImage;
    [SerializeField]
    private CanvasGroup draftCanvas;
    [SerializeField]
    private DraftUI draftUI;
    [SerializeField]
    private Button toggleButton;
    
    private RoundManager.Round.RoundType roundType;
    private Character player;
    private RoundManager roundManager;

    private void Start()
    {
        roundManager = GameManager.GetManager<RoundManager>();
        roundManager.CombatRoundTimer.OnValueChanged += OnCombatTimerUpdated;

        EventBus.StartListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, Init);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, StartCombat);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, StartDraft);
        EventBus.StartListening<DraftPickData>(EventBusEnum.EventName.DRAFT_TIMER_UPDATED_CLIENT, OnDraftTimerUpdated);

        draftUI.DraftPickedEvent += OnDraftCardPicked;
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.CombatRoundTimer.OnValueChanged -= OnCombatTimerUpdated;
        }

        EventBus.StopListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, Init);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, StartCombat);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, StartDraft);
        EventBus.StopListening<DraftPickData>(EventBusEnum.EventName.DRAFT_TIMER_UPDATED_CLIENT, OnDraftTimerUpdated);
        if (draftUI != null)
        {
            draftUI.DraftPickedEvent -= OnDraftCardPicked;
        }
    }


    private void Init()
    {
        player = GameManager.GetManager<CharacterManager>().PlayerCharacter;

        draftUI.Intialise(GlobalGameSettings.Settings.DraftCardsPerPack);
        draftUI.SetToggleButton(toggleButton);
    }

    private void OnDraftTimerUpdated(DraftPickData timerData)
    {
        roundInfoText.text = "Choose your Destiny";
        roundTimerText.text = timerData.timer.ToString();
        roundTimerImage.fillAmount = (float) timerData.timer / (float) timerData.totalDuration;
    }

    private void OnCombatTimerUpdated(int previous, int current)
    {
        roundTimerText.text = current.ToString();
        roundTimerImage.fillAmount = (float) current / (float)roundManager.CurrentRound.secondsLong;
    }

    private void OnDraftCardPicked()
    {
        if(!draftUI.HasPack && !player.IsInCombat.Value)
        {
            roundInfoText.text = "Waiting for other players...";
            roundTimerText.text = "";
            roundTimerImage.fillAmount = 1;
        }
    }

    private void StartCombat()
    {
        roundType = RoundManager.Round.RoundType.Combat;
        draftCanvas.alpha = 0;
        draftCanvas.interactable = false;
        draftCanvas.blocksRaycasts = false;
        roundInfoText.text = "Combat";
        draftUI.SetToggleButtonVisibility(false);
    }

    private void StartDraft()
    {
        roundType = RoundManager.Round.RoundType.Draft;
        roundInfoText.text = "Draft Start";
    }
}
