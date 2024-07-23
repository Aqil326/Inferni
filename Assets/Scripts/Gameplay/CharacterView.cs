using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

public class CharacterView : TargetableView, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Character character;

    [SerializeField]
    private CastingIcon castingIcon;

    [SerializeField]
    private HealthBar healthBar;

    [SerializeField]
    private DeathTimerBar deathTimerBar;

    [SerializeField]
    private Image teamIndicatorImage;

    [SerializeField]
    private GameObject nonPlayerInfo;

    [SerializeField]
    private TextMeshProUGUI handSizeText;

    [SerializeField]
    private EnergyPoolViewEntry energyPoolViewPrefab;

    [SerializeField]
    private Transform energyPoolViewParent;

    [SerializeField]
    private DiamondShapedGrid charmSlotGrid;

    [SerializeField]
    private Transform charmSlotUIParent;

    [SerializeField]
    private GameObject selectedHighlight;

    [SerializeField]
    private SpriteRenderer[] selectedHighlightSprites;

    [SerializeField]
    public Color unselectedColor;
    
    [SerializeField]
    public Color selectedColor;

    [SerializeField]
    private SoundManager.Sound selectedSound;

    [SerializeField]
    private GameObject playerIndicator;

    [SerializeField]
    private ModifierUI modifierUIPrefab;

    [SerializeField]
    private Transform modifierUIParent;

    [SerializeField]
    private IndicatorText indicatorTextPrefab;
    
    [SerializeField]
    private Transform indicatorTextPosition;

    [SerializeField]
    private GameObject nemesisIndicator;

    private ObjectPool<IndicatorText> indicatorTextPool;

    
    [SerializeField]
    private Transform card3DParent;

    [SerializeField]
    private Transform modelParent;
    
    [SerializeField]
    public Transform boosterTarget;

    [SerializeField]
    private TextMeshProUGUI playerName;
    
    [SerializeField]
    private GameObject highestIndicator;
    [SerializeField]
    private GameObject lowestIndicator;

    [SerializeField]
    private Vector3 draftPackPositionOffsetMultiplier = new Vector3(.1f, 0, -.01f);

    [SerializeField]
    private SoundManager.Sound knockedSound;

    [SerializeField]
    private SoundManager.Sound reviveSound;

    [SerializeField]
    private SoundManager.Sound deadSound;

    [SerializeField]
    private Collider collider;

    [SerializeField]
    private Image teamBG;

    [SerializeField]
    private Particle damageParticle;

    [SerializeField]
    private Particle healParticle;

    [SerializeField]
    private Transform projectileSpawnPoint;

    public Character Character => character;
    public CharacterModel CharacterModel { get; private set; }

    private ModifierUI modifierUI;
    public GameObject ModifierEffectUI;

    private CharacterManager characterManager;
    private RoundManager roundManager;
    private GameObject characterModelObject;

    private List<EnergyPoolViewEntry> energyPoolsViews = new List<EnergyPoolViewEntry>();
    private bool isPointerOver;
    private int initialLayer;
    private bool isPaused = false;

    public event Action PauseEvent;
    public event Action UnpauseEvent;

    public void Init()
    {
        characterManager = GameManager.GetManager<CharacterManager>();
        roundManager = GameManager.GetManager<RoundManager>();

        SetTargetable(Character);

        selectedHighlight.SetActive(false);
        highestIndicator.SetActive(false);
        lowestIndicator.SetActive(false);
        
        characterManager.OnHighestPlayer += SetHighestHealth;
        characterManager.OnLowestPlayer += SetLowestHealth;

        if (character != characterManager.PlayerCharacter)
        {
            var charmSlots = Character.CharmSlots;
            charmSlotGrid.Init<CharmSlotUI>(charmSlots.Length, (int index, CharmSlotUI slotUI) =>
            {
                slotUI.Init(charmSlots[index]);
            });
        }

        playerName.text = Character.PlayerData.DisplayName;

        castingIcon.Init(Character);
        healthBar.Init(Character);
        deathTimerBar.Init(character);

        characterModelObject = Instantiate(character.Data.CharacterModel, modelParent);
        CharacterModel = characterModelObject.GetComponent<CharacterModel>();
        CharacterModel.Initialise();

        character.HandChangedEvent += UpdateHandSize;
        character.ModifierAddedEvent += OnModifierAdded;
        character.ModifierRemovedEvent += OnModifierRemoved;
        character.CardCastingStartEvent += BeginCast;
        character.CardCastingEndEvent += EndCast;
        character.CardPlayedEvent_Client += ThrowProjectile;
        character.Health.OnValueChanged += HealthChanged;
        character.State.OnValueChanged += CharacterStateChange;
        characterManager.PlayerCharacter.NemesisIndex.OnValueChanged += CheckForNemesis;

        playerIndicator.SetActive(Character.IsPlayer());

        modifierUI = Instantiate(modifierUIPrefab, modifierUIParent);
        modifierUI.Clear();

        damageParticle.gameObject.SetActive(false);
        healParticle.gameObject.SetActive(false);

        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnCombatStarted);
        EventBus.StartListening<Character>(EventBusEnum.EventName.CHARACTER_KILLED_NEMESIS_CLIENT, OnCharacterDefeatedNemesis);
        initialLayer = CharacterModel.gameObject.layer;

        UpdateHandSize();
        CheckForNemesis(0, 0);

        IndicatorText CreateIndicatorFunc()
        {
            return Instantiate(indicatorTextPrefab, indicatorTextPosition);
        }

        void ActionOnGetIndicator(IndicatorText indicator)
        {
            indicator.gameObject.SetActive(true);
        }

        void ActionOnReleaseIndicator(IndicatorText indicator)
        {
            indicator.gameObject.SetActive(false);
        }

        void ActionOnDestroyIndicator(IndicatorText indicator)
        {
            Destroy(indicator.gameObject);
        }

        indicatorTextPool = new ObjectPool<IndicatorText>(CreateIndicatorFunc, ActionOnGetIndicator, ActionOnReleaseIndicator, ActionOnDestroyIndicator);
    }


    public void SetTeamInfo()
    {
        var sprite = GlobalGameSettings.Settings.GetTeamSprite(Character.TeamIndex);
        teamIndicatorImage.sprite = sprite;
        var color = GlobalGameSettings.Settings.GetTeamColor(Character.TeamIndex);
        teamBG.color = color;
    }

    public void AddCard3D(Card3D card)
    {
        card.transform.SetParent(card3DParent);
        card.transform.localPosition = Vector3.zero;
    }

    private void UpdateHandSize()
    {
        handSizeText.text = Character.HandSize.ToString();
    }

    private void SetSelectionRingStatus(bool isHighlighted)
    {
        foreach (var sprite in selectedHighlightSprites)
        {
            sprite.color = isHighlighted ? selectedColor : unselectedColor;
        }
    }

    private void OnModifierAdded(CharacterModifier modifier)
    {
        modifierUI.AddModifier(modifier.Data, modifier.Duration, Character.RemainingModifierTime);
        modifier.ApplyVisualEffects(this, modifier.Data.pauseAnimation);
    }

    private void OnModifierRemoved(CharacterModifier modifier)
    {
        modifier.RemoveVisualEffects(this, modifier.Data.pauseAnimation);
        modifierUI.Clear();
    }

    private void CharacterStateChange(CharacterState oldState, CharacterState newState)
    {
        if (oldState == CharacterState.Downed && newState == CharacterState.Alive)
        {
            CharacterModel.Revive();
            reviveSound.Play(false);
        }
        else if(newState == CharacterState.Downed)
        {
            CharacterModel.Downed();
            knockedSound.Play(false);
        }
        else if (newState == CharacterState.Dead)
        {
            CharacterModel.Death();
            deadSound.Play(false);
        }
    }

    int totalHealthDelta = 0;
    Coroutine healthIndicatorCoroutine;
    private void HealthChanged(int oldHealth, int newHealth)
    {
        int delta = newHealth - oldHealth;
        if (delta == 0) return;

        totalHealthDelta += delta;
        if (healthIndicatorCoroutine == null)
        {
            healthIndicatorCoroutine = StartCoroutine(ShowHealthChangeIndicator(delta));
        }

        if (newHealth > 0)
        {
            if (delta < 0)
            {
                //CharacterModel.Damage();
                damageParticle.gameObject.SetActive(true);
                damageParticle.Reset();
            }
            else
            {
                //CharacterModel.Heal();
                healParticle.gameObject.SetActive(true);
                healParticle.Reset();
            }
        }
    }

    private IEnumerator ShowHealthChangeIndicator(int delta)
    {
        yield return null;
        var indicator = indicatorTextPool.Get();
        indicator.ShowIndicator(totalHealthDelta, OnIndicatorAnimationFinished);
        healthIndicatorCoroutine = null;
        totalHealthDelta = 0;
    }

    private void OnIndicatorAnimationFinished(IndicatorText indicator)
    {
        indicatorTextPool.Release(indicator);
    }

    private void OnDraftStarted()
    {
        Pause(true);
    }

    private void OnCombatStarted()
    {
        Pause(false);
    }

    private void BeginCast(int cardIndex, float timer, CastingType castingType)
    {
        if (isPaused) return;
        CharacterModel.Cast(Character.Hand[cardIndex]);
        CharacterModel.SetLookTowards(Vector3.zero);
    }

    private void EndCast()
    {
        if (isPaused) return;
        CharacterModel.EndCast();
    }

    private void ThrowProjectile(string cardId, bool selfTarget, ITargetable target)
    {
        if (isPaused) return;
        if (selfTarget)
        { 
            CharacterModel.SetLookTowards(Vector3.zero);
        }
        else
        {
            CharacterModel.SetLookTowards(target.Position);
        }
        CharacterModel.Throw(selfTarget);
    }

    public void SetHighestHealth(Character highChar)
    {
        if (highChar != Character)
        {
            highestIndicator.SetActive(false);
        }
        else
        {
            highestIndicator.SetActive(true);
        }
    }

    public void SetLowestHealth(Character lowChar)
    {
        if (lowChar != Character)
        {
            lowestIndicator.SetActive(false);
        }
        else
        {
            lowestIndicator.SetActive(true);
        }
    }


    public void AddModifierEffect(GameObject prefab)
    {
        ModifierEffectUI = Instantiate(prefab, characterModelObject.transform);
        ModifierEffectUI.transform.localPosition = Vector3.zero;
    }

    public void RemoveModifierEffect()
    {
        if (ModifierEffectUI != null)
        {
            Destroy(ModifierEffectUI);
        }
    }

    public override bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        if (Character == null)
        {
            Debug.Log($"Character is null? {transform.parent}");
        }
        return base.ShouldOverrideTargetAvailability(ref canBeTargeted, card);
    }

    public override void TargetSelected(bool isValidTarget)
    {
        selectedSound.Play(false);
        selectedHighlight.SetActive(true);
        SetSelectionRingStatus(isValidTarget);
        CharacterModel.ShowSelected();
    }

    public override void TargetDeselected()
    {
        selectedHighlight.SetActive(false);
        CharacterModel.HideSelected();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
        LineManager.Instance.ShowLines(Character);
        isPointerOver = true;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
        if (!isPointerOver) return;

        LineManager.Instance.HideLines(Character, null);
        isPointerOver = false;
    }
    
    public Vector3 GetAvailablePackPosition()
    {
        return GetPackPosition(Character.DraftPackBacklog.Count);
    }

    public Vector3 GetPackPosition(int packIndex)
    {
        var position = boosterTarget.position;
        position.x = position.x - draftPackPositionOffsetMultiplier.x * packIndex;
        position.y = position.y - draftPackPositionOffsetMultiplier.y * packIndex;
        position.z = position.z - draftPackPositionOffsetMultiplier.z * packIndex;
        return position;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Character != null)
        {
            Character.HandChangedEvent -= UpdateHandSize;
            Character.ModifierAddedEvent -= OnModifierAdded;
            Character.ModifierRemovedEvent -= OnModifierRemoved;
            Character.CardCastingStartEvent -= BeginCast;
            Character.CardPlayedEvent_Client -= ThrowProjectile;
            Character.Health.OnValueChanged -= HealthChanged;
            Character.State.OnValueChanged -= CharacterStateChange;
            
        }

        if(characterManager != null && characterManager.PlayerCharacter != null)
        {
            characterManager.PlayerCharacter.NemesisIndex.OnValueChanged -= CheckForNemesis;
        }

        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, OnDraftStarted);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, OnCombatStarted);
        EventBus.StopListening<Character>(EventBusEnum.EventName.CHARACTER_KILLED_NEMESIS_CLIENT, OnCharacterDefeatedNemesis);
    }

    public void Pause(bool pause)
    {
        isPaused = pause;
        CharacterModel.Pause(pause);

        if(pause)
        {
            PauseEvent?.Invoke();
        }
        else
        {
            UnpauseEvent?.Invoke();
        }
    }

    private void CheckForNemesis(int previous, int current)
    {
        var showNemesis = !GameManager.Instance.DisableRP &&
                          characterManager.PlayerCharacter.NemesisIndex.Value == character.Index;
        nemesisIndicator.SetActive(showNemesis);
    }

    private void OnCharacterDefeatedNemesis(Character character)
    {
        if(character.Index == Character.Index)
        {
            //TODO: Trigger nemesis defeated animation here
        }
    }

    protected override void OnCardDragStarted(Card card)
    {
        bool isDragTarget = false;
        switch (card.Data.Target)
        {
            case CardTarget.AnyOpponent:
                isDragTarget = character.TeamIndex != characterManager.PlayerCharacter.TeamIndex;
                break;
            case CardTarget.AnyPlayer:
            case CardTarget.Any:
                isDragTarget = card.IsCastable();
                break;
            case CardTarget.DownedPlayer:
                isDragTarget = character.State.Value == CharacterState.Downed;
                break;
            case CardTarget.Teammate:
                isDragTarget = character.TeamIndex == characterManager.PlayerCharacter.TeamIndex && character != characterManager.PlayerCharacter;
                break;
            case CardTarget.Self:
                isDragTarget = character == characterManager.PlayerCharacter;
                break;
            default:
                isDragTarget = false;
                break;
        }


        if (isDragTarget)
        {
            isDragTarget = character.IsTargetable.Value;
        }

        collider.enabled = isDragTarget;

        if(isDragTarget)
        {
            CharacterModel.ShowOutline();
            CharacterModel.gameObject.SetGameLayerRecursive(LayerMask.NameToLayer("Outline"));
        }

        bool isCharmTarget = card.Data.Target == CardTarget.Charm ||
            card.Data.Target == CardTarget.EmptyCharmSlot ||
            card.Data.Target == CardTarget.Any;

        charmSlotUIParent.gameObject.SetGameLayerRecursive(LayerMask.NameToLayer("Outline"));
    }

    protected override void OnCardDragEnded(Card card)
    {
        if(CharacterModel.gameObject.layer != initialLayer)
        {
            CharacterModel.gameObject.SetGameLayerRecursive(initialLayer);
        }
        CharacterModel.HideOutline();
        collider.enabled = true;

        if(charmSlotUIParent.gameObject.layer != initialLayer)
        {
            charmSlotUIParent.gameObject.SetGameLayerRecursive(initialLayer);
        }
    }

}
