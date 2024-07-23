using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PromptController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI promptText;

    [SerializeField]
    private float textAnimationDuration = 0.5f;

    [SerializeField]
    private float textShowDuration = 1f;

    [SerializeField]
    private Transform textInitialPos;

    [SerializeField]
    private Transform textShowPos;

    [SerializeField]
    private Transform textEndPos;

    [SerializeField]
    private string draftIncomingText;

    [SerializeField]
    private string combatIncomingText;

    [SerializeField]
    private string deathTickIncomingText;

    [SerializeField]
    private string draftStartText;

    [SerializeField]
    private string combatStartText;

    [SerializeField]
    private string deathTickStartText;

    [SerializeField]
    private int[] draftWarningTimes;

    [SerializeField]
    private int[] deathTickWarningTimes;

    [SerializeField]
    private string nemesisDefeatedText;

    private event Action textAnimationFinishedEvent;

    private RoundManager roundManager;
    private CharacterManager characterManager;
    private HashSet<int> draftWarningTimesSet = new HashSet<int>();
    private HashSet<int> deathTickWarningTimesSet = new HashSet<int>();
    private Sequence textAnimation;
    private bool isTextArriving;
    private bool isTextRetreating;


    public void Init()
    {
        roundManager = GameManager.GetManager<RoundManager>();
        characterManager = GameManager.GetManager<CharacterManager>();
        roundManager.CombatRoundTimer.OnValueChanged += OnCombatTimerChanged;
        roundManager.DraftEndTimer.OnValueChanged += OnDraftEndTimer;
        EventBus.StartListening(EventBusEnum.EventName.START_DEATH_TICK_CLIENT, OnDeathTickStarted);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnCombatStarted);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StartListening<Character>(EventBusEnum.EventName.CHARACTER_KILLED_NEMESIS_CLIENT, OnCharacterKilledNemesis);

        foreach (var i in draftWarningTimes)
        {
            draftWarningTimesSet.Add(i);
        }

        foreach(var i in deathTickWarningTimes)
        {
            deathTickWarningTimesSet.Add(i);
        }
    }

    public void ShowText(string text)
    {
        promptText.text = text;
        if (textAnimation == null)
        {
            promptText.transform.position = textInitialPos.position;
            promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, 0);
            isTextArriving = true;
            textAnimation = DOTween.Sequence().Append(promptText.transform.DOMove(textShowPos.position, textAnimationDuration))
                .Join(promptText.DOFade(1, textAnimationDuration))
                .Append(TextRetreatAnimation());
        }
        else if(isTextRetreating)
        {
            textAnimationFinishedEvent += () => { ShowText(text); };
        }
        else if(!isTextArriving)
        {
            promptText.text = text;
            textAnimation.Kill();
            textAnimation = TextRetreatAnimation();
        }
    }

    private Sequence TextRetreatAnimation()
    {
        return DOTween.Sequence().AppendCallback(() => { isTextArriving = false; })
            .AppendInterval(textShowDuration)
            .AppendCallback(() => { isTextRetreating = true; })
            .Append(promptText.transform.DOMove(textEndPos.position, textAnimationDuration))
            .Join(promptText.DOFade(0, textAnimationDuration))
            .AppendCallback(() => {
                isTextRetreating = false;
                textAnimation = null;
                textAnimationFinishedEvent?.Invoke();
                if (textAnimationFinishedEvent != null)
                {
                    foreach (Delegate d in textAnimationFinishedEvent.GetInvocationList())
                    {
                        textAnimationFinishedEvent -= (Action)d;
                    }
                }
            });
    }

    private void OnDeathTickStarted()
    {
        ShowText(deathTickStartText);
    }

    private void OnCombatStarted()
    {
        ShowText(combatStartText);
    }

    private void OnDraftStarted()
    {
        ShowText(draftStartText);
    }

    private void OnCombatTimerChanged(int previous, int current)
    {
        if (roundManager.IsLastRound)
        {
            if (deathTickWarningTimesSet.Contains(current))
            {
                ShowText(deathTickIncomingText.Replace("{0}", current.ToString()));
            }
        }
        else
        {
            if (draftWarningTimesSet.Contains(current))
            {
                ShowText(draftIncomingText.Replace("{0}", current.ToString()));
            }
        }
    }

    private void OnDraftEndTimer(int previous, int current)
    {
        ShowText(combatIncomingText.Replace("{0}", current.ToString()));
    }

    private void OnCharacterKilledNemesis(Character character)
    {
        if(character == characterManager.PlayerCharacter)
        {
            ShowText(nemesisDefeatedText);
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.CombatRoundTimer.OnValueChanged -= OnCombatTimerChanged;
            roundManager.DraftEndTimer.OnValueChanged -= OnDraftEndTimer;
        }
        EventBus.StopListening(EventBusEnum.EventName.START_DEATH_TICK_CLIENT, OnDeathTickStarted);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnCombatStarted);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StopListening<Character>(EventBusEnum.EventName.CHARACTER_KILLED_NEMESIS_CLIENT, OnCharacterKilledNemesis);
    }
}
