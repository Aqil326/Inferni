using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using TMPro;

public struct CastEventParams
{
    public Card card;
    public ITargetable target;
}

public enum CastingType
{
    Spell,
    Energy,
    CardSend
}

public class Character : NetworkBehaviour, ITargetable
{
    [SerializeField]
    private CharacterView characterView;

    [SerializeField]
    private CastingTargetable castingTargetable;

    [SerializeField]
    private Vector3 targetableOffset = new Vector3(0f, 0.65f, 0f);

    [SerializeField]
    private Transform projectileSpawnPoint;

    [SerializeField]
    private Transform projectileSpawnPointParent;

    public EnergyPool[] EnergyPools;

    public bool IsSpectating;

    public NetworkVariable<int> DeckSize = new NetworkVariable<int>();
    public NetworkVariable<int> DiscardPileSize = new NetworkVariable<int>();
    public NetworkVariable<int> Health = new NetworkVariable<int>();
    public NetworkVariable<int> MaxHealth = new NetworkVariable<int>();
    public NetworkVariable<float> CastingTimeChange = new NetworkVariable<float>();
    public NetworkVariable<int> DeathTimer = new NetworkVariable<int>();
    public NetworkVariable<CharacterState> State = new NetworkVariable<CharacterState>();
    public NetworkVariable<bool> IsCasting = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsInCombat = new NetworkVariable<bool>();

    public NetworkVariable<bool> DisableCardPlay = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsTargetable = new NetworkVariable<bool>();
    public NetworkVariable<float> RemainingCastingTime = new NetworkVariable<float>();
    public NetworkVariable<float> RemainingDrawTime = new NetworkVariable<float>();
    public NetworkVariable<float> RemainingModifierTime = new NetworkVariable<float>();
    public NetworkVariable<int> NemesisIndex = new NetworkVariable<int>();
    //These properties are set by the server and synced to Clients via ClientRPC
    public int ProjectileIndex { get; protected set; }
    public bool IsDead => State.Value == CharacterState.Dead;
    public int Index { get; set; }
    public int TeamIndex { get; set; }
    public Card[] Hand { get; protected set; }
    public CharmSlot[] CharmSlots { get; protected set; }
    public int HandSize { get; protected set; }
    public PlayerData PlayerData { get; protected set; }
    public CharacterData Data { get; protected set; }
    public CharacterModifier Modifier { get; protected set; }
    public Vector3 ProjectileSpawnPosition => projectileSpawnPoint.position;

    //Server only
    public List<DraftPack> DraftPackBacklog { get; protected set; } = new List<DraftPack>();
    public Character Nemesis { get; protected set; }
    private List<CardHistoryRecord> CastCardHistory { get; set; } = new List<CardHistoryRecord>();
    private List<DamageHistoryRecord> DealtDamageHistory { get; set; } = new List<DamageHistoryRecord>();
    private List<DamageHistoryRecord> ReceivedDamageHistory { get; set; } = new List<DamageHistoryRecord>();

    public CastingTargetable CastingTargetable => castingTargetable;
    public CharacterView CharacterView => characterView;
    public int MaxDeathTimer => Data.MaxDeathTimer;
    private Coroutine deathTimerCoroutine;
    private Coroutine healAndDamageCoroutine;
    private bool hasDownedNemesis;
    
    //Client only events
    public event Action<string, bool, ITargetable> CardPlayedEvent_Client;

    //Client and Server Events
    public event Action CardDrawStartEvent;
    public event Action CardDrawEndEvent;
    public event Action<Card, int> CardDrawnEvent;
    public event Action<Card, int, Vector3> CardDrawnWithPositionEvent;
    public event Action<Card, Vector3> CardAddToDeckWithPositionEvent;
    public event Action<Card, int, float> CardDiscardedEvent;
    public event Action<Card, int> CardBatchDiscardedEvent;
    public event Action<int> CardDestroyedEvent;
    public event Action HandChangedEvent;
    public event Action<int, float, CastingType> CardCastingStartEvent;
    public event Action CardCastingEndEvent;
    public event Action<Character> StateChangedEvent;
    public event Action<Card, Character> DeathEvent;
    public event Action CardPlayabilityChangedEvent;
    public event Action<CharacterModifier> ModifierAddedEvent;
    public event Action<CharacterModifier> ModifierRemovedEvent;

    //Server only events
    public event Action<Card> CardPlayedEvent_Server;
    public event Action<Character, int, Card> HealthChangedByCardEvent;
    public event Action<Projectile> ProjectileFiredEvent_Server;

    protected GlobalGameSettings gameSettings => GlobalGameSettings.Settings;

    string ITargetable.TargetId => "Character" + Index;
    Vector3 ITargetable.Position => transform.position + targetableOffset;

    public object PositionId { get; internal set; }

    protected CharacterManager characterManager;
    protected CardsEffectsManager cardEffectsManager;
    protected RoundManager roundManager;

    //Client and server
    protected Dictionary<string, ITargetable> subTargetables = new Dictionary<string, ITargetable>();

    //Server only
    protected Deck deck;
    protected List<Card> discardPile = new List<Card>();
    protected Coroutine drawCoroutine;
    protected Coroutine castCoroutine;
    protected bool ignoreCastRequirement;

    public virtual void Init(PlayerData player)
    {
        characterManager = GameManager.GetManager<CharacterManager>();
        cardEffectsManager = GameManager.GetManager<CardsEffectsManager>();
        roundManager = GameManager.GetManager<RoundManager>();

        Data = GameDatabase.GetCharacterData(player.SelectedCharacterId);

        PlayerData = player;
        TeamIndex = player.TeamIndex;

        Hand = new Card[gameSettings.StartHandSize];

        CharmSlots = new CharmSlot[gameSettings.CharmSlotsNumber];
        for (int i = 0; i < CharmSlots.Length; i++)
        {
            var slot = new CharmSlot(i, this);
            CharmSlots[i] = slot;
            subTargetables.Add(((ITargetable)slot).TargetId, slot);
            slot.CharmRemovedEvent += OnCharmRemoved;
        }

        DraftPackBacklog = new List<DraftPack>();

        castingTargetable.Init(this);
        AddSubtargetable(castingTargetable);

        InitEnergyPools(); 

        IsInCombat.OnValueChanged += RaiseChangeCardPlayabilityEvent;
        DisableCardPlay.OnValueChanged += RaiseChangeCardPlayabilityEvent;
        State.OnValueChanged += RaiseStateChangedEvent;

        if (IsServer)
        {
            NemesisIndex.Value = -1;
            MaxHealth.Value = Data.MaxHealth;
            Health.Value = MaxHealth.Value;
            IsTargetable.Value = true;
            var decklist = new List<CardData>(Data.StartingCards);
            deck = new Deck(decklist, this);
            InitClientRpc(player);
            DrawInitialHand();

            if (Data.StartingCharms != null)
            {
                foreach (var charm in Data.StartingCharms)
                {
                    AddCharm(charm.CharmData, charm.Index);
                }
            }
        }
    }

    [ClientRpc]
    private void InitClientRpc(PlayerData player)
    {
        if (IsHost)
        {
            return;
        }
        Init(player);
    }

    public void InitView()
    {
        CharacterView.Init();
    }

    private void OnEnable()
    {
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StartListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StartListening(EventBusEnum.EventName.END_ROUND_DRAFT_SERVER, DrawToHandSize);
        EventBus.StartListening<int>(EventBusEnum.EventName.DEATH_TICK_APPLIED_SERVER, DeathTickApplied);
    }
    
    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StopListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StopListening(EventBusEnum.EventName.END_ROUND_DRAFT_SERVER, DrawToHandSize);
        EventBus.StopListening<int>(EventBusEnum.EventName.DEATH_TICK_APPLIED_SERVER, DeathTickApplied);
    }

    private void ResetCards()
    {
        //Discard hand
        for (int i = 0; i < Hand.Length; i++)
        {
            // When resume from draft to combat, if card is in-casting or drag to Mana pool
            // the Hand[i] is already null and in the Discard Pile
            if (Hand[i] != null)
            {
                DiscardCard(i);    
            }
        }

        ResetDeck();

        DrawInitialHand();
    }


    private void DrawToHandSize()
    {
        for (int i = 0; i < Hand.Length; i++)
        {
            if (Hand[i] == null)
            {
                DrawCard();
            }
        }
    }

    private void StartCombatRound()
    {
        if (IsServer)
        {
            IsInCombat.Value = true;

            foreach (var pool in EnergyPools)
            {
                pool.Unpause();
            }

            if (Modifier != null)
            {
                Modifier.Unpause();
            }
        }
    }

    private void EndCombatRound()
    {
        if (IsServer)
        {
            IsInCombat.Value = false;
            if (drawCoroutine != null)
            {
                ClearDrawCoroutine();
            }

            foreach (var pool in EnergyPools)
            {
                pool.Pause();
            }

            if (Modifier != null)
            {
                Modifier.Pause();
            }
        }
    }

    public void ReceiveDraftPack(DraftPack draftPack)
    {
        DraftPackBacklog.Add(draftPack);

        if (IsServer)
        {
            ReceiveDraftPackClientRPC(draftPack.GetData(CharacterView.GetAvailablePackPosition()));
        }
    }

    [ClientRpc]
    private void ReceiveDraftPackClientRPC(DraftPackData draftPackData)
    {
        EventBus.TriggerEvent<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_PASS_CLIENT, draftPackData);
    }


    public bool IsPlayer()
    {
        return PlayerData.Id == NetworkManager.Singleton.LocalClientId;
    }

    protected virtual void InitEnergyPools()
    {
        for (int i = 0; i < EnergyPools.Length; i++)
        {
            EnergyPool pool = EnergyPools[i];
            pool.Init(this, i);
            AddSubtargetable(pool);
        }
    }

    protected virtual void DrawInitialHand()
    {
        for (int i = 0; i < gameSettings.StartHandSize; i++)
        {
            DrawCard();
        }
    }
    
    public void DrawCardsWithWait(int addCardsCount, float timeWait)
    {
        StartCoroutine(DrawCardsWithWaitCoroutine(addCardsCount, timeWait));
    }
    
    private IEnumerator DrawCardsWithWaitCoroutine(int addCardsCount, float timeWait)
    {
        yield return new WaitForSeconds(timeWait);
        for (int i = 0; i < addCardsCount; i++)
        {
            if (!CanDrawCards())
            {
                yield break;
            }
            
            DrawCard();
        }
    }

    private void Update()
    {
        if(!IsInCombat.Value)
        {
            return;
        }

        for (int i = 0; i < Hand.Length; i++)
        {
            Card card = Hand[i];
            if (card != null)
            {
                card.UpdateTimeInHand(Time.deltaTime);
                if (card.TryUpdateCardXData(CardXPreviewType.Initial))
                {
                    UpdateCardXDataClientRPC(i, card.XData);
                }
            }
        }
    }

    [ClientRpc]
    public void UpdateCardXDataClientRPC(int handIndex, XData xData)
    {
        if(IsHost)
        {
            return;
        }

        Hand[handIndex].XData = xData;
    }

    public void DrawCard()
    {
        if (!CanDrawCards())
        {
            return;
        }

        InternalDrawCard();
    }

    public void TransformCardInHand(int indexToTransform, string cardId)
    {
        var card = new Card(GameDatabase.GetCardData(cardId));
        card.SetOwner(this);
        
        if (!IsHost)
        {
            Hand[indexToTransform] = card;
            card.SetHandIndex(indexToTransform);
        }
        
        if (IsServer)
        {
            TransformCardInHandClientRpc(indexToTransform, cardId);
        }
    }
    
    [ClientRpc]
    private void TransformCardInHandClientRpc(int indexToTransform, string cardId)
    {
        var card = new Card(GameDatabase.GetCardData(cardId));
        card.SetOwner(this);
        card.SetHandIndex(indexToTransform);
        StartCoroutine(StartAnimationTransform(indexToTransform, card));
    }
    
    IEnumerator StartAnimationTransform(int indexToTransform, Card newCard)
    {
        CardUI cardUI = Hand[indexToTransform].CardUI;
        if (cardUI == null) yield break;
        bool wait = true;
        cardUI.PlayFlipAnimation(() =>
        {
            Hand[indexToTransform] = newCard;
            cardUI.SetHandCard(newCard, true, true);
            wait = false;
        });
        while (wait)
        {
            yield return new WaitForEndOfFrame();
        }
        
        cardUI.PlayFlipAnimation(null);
    }
    
    public Card RemoveCardFromDiscardPile(Card card)
    {
        if (card.OriginalOwner != this) return null;
        
        for (int i = discardPile.Count - 1; i >= 0; i--)
        {
            Card tmpCard = discardPile[i];
            if (tmpCard.Data == card.Data)
            {
                discardPile.Remove(tmpCard);
                DiscardPileSize.Value = discardPile.Count;
                HandChangedEvent?.Invoke();
                return tmpCard;
            }
        }

        return null;
    }
    
    protected void ResetDeck()
    {
        deck.AddCards(discardPile);
        discardPile.Clear();
        deck.Shuffle();
        DeckSize.Value = deck.DeckSize;
        DiscardPileSize.Value = discardPile.Count;
    }

    protected virtual int InternalDrawCard()
    {
        if (deck.DeckSize == 0)
        {
            ResetDeck();
        }

        var card = deck.Draw();
        int index = AddCardToHand(card);
        DeckSize.Value = deck.DeckSize;

        if (drawCoroutine != null && HandSize == Hand.Length)
        {
            ClearDrawCoroutine();
        }
        return index;
    }

    public void AddCardToHandWithPosition(Card card, Vector3 worldPosition)
    {
        EventBus.TriggerEvent<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_HAND_WITH_POSITION_SERVER, new CardAddedData()
        {
            card = card,
            currentPosition = worldPosition,
            targetCharacter = this
        });
        return;
    }

    public void AddCardToHand(Card card, int handIndex, Vector3? animationPosition = null)
    {
        Hand[handIndex] = card;
        card.SetOwner(this);
        card.SetHandIndex(handIndex);
        AddSubtargetable(card);
        HandSize++;
        HandChangedEvent?.Invoke();

        if (animationPosition.HasValue)
        {
            CardDrawnWithPositionEvent?.Invoke(card, handIndex, animationPosition.Value);
        }
        else
        {
            CardDrawnEvent?.Invoke(card, handIndex);
        }
        
        if (IsServer)
        {
            AddCardToHandClientRPC(card.Data.InternalID, handIndex, animationPosition.HasValue, animationPosition.HasValue? animationPosition.Value : Vector3.zero);
        }
    }

    [ClientRpc]
    private void AddCardToHandClientRPC(string cardId, int handIndex, bool hasAnimationPosition, Vector3 animationPosition)
    {
        if(IsHost)
        {
            return;
        }

        var card = new Card(GameDatabase.GetCardData(cardId));
        AddCardToHand(card, handIndex, hasAnimationPosition? animationPosition : null);
    }

    public int AddCardToHand(Card card, Vector3? animationPosition = null)
    {
        int index = GetEmptyCardSlot();
        if (index >= 0)
            AddCardToHand(card, index, animationPosition);
        else
            Debug.LogError("Couldn't add card to hand!");
        return index;
    }

    private int GetEmptyCardSlot()
    {
        for (int i = 0; i < Hand.Length; i++)
        {
            if (Hand[i] == null)
            { 
                return i;
            }
        }
        return -1;
    }


    public bool CanPlayCards()
    {
        if (characterManager.NumberOfTeamsAlive == 1) 
        {
            return false;
        }

        if(IsCasting.Value)
        {
            return false;
        }

        if (IsDead || State.Value == CharacterState.Downed)
        {
            return false;
        }
        
        if (DisableCardPlay.Value)
        {
            return false;
        }

        if(!IsInCombat.Value)
        {
            return false;
        }
        
        return true;
    }
    
    private bool CanPlayCard(int index)
    {
        if (!CanPlayCards()) return false;
        
        if(index < 0 || index >= Hand.Length)
        {
            Debug.LogError("Invalid Card Index: " + index);
            return false;
        }
        
        if (Hand[index] == null)
        {
            Debug.Log("No card in this slot");
            return false;
        }

        return true;
    }

    public bool TryPlayCardFromServer(int index, ITargetable targetable)
    {
        if (!CanPlayCard(index)) return false;
        
        var card = Hand[index];
        if (card.Data.IsBlocked) return false;

        if (!card.IsCastable()) return false;

        if (!card.IsTargetableValid(targetable)) return false;
        
        StartCastingServerRPC(index, targetable.TargetId, Hand[index].TimeInHand, true);
        return true;
    }
    
    //Client only
    public bool TryPlayCard(int index, TargetableView target, bool skipEnergyCheck = false)
    {
        if (!CanPlayCard(index)) return false;
        
        var card = Hand[index];

        if (card.Data.IsBlocked)
        {
            return false;
        }
        
        if (target is not DiscardPileUI && target is not EnergyPoolUI && !card.IsCastable())
        {
            return false;
        }

        if (target is DiscardPileUI)
        {
            DiscardCard(index);
            return true;
        }


        if (target is SendCardToAllyUI)
        {
            if (SendCardToAlly(index))
            {
                return true;
            }
            return false;
        }


        // Will not be called anymore for the NoMana version
        if (target is EnergyPoolUI poolUI)
        {
            if (!poolUI.Pool.CanAddCard(card))
            {
                return false;
            }
            StartCastingEnergyServerRPC(index, poolUI.Pool.Index);
            return true;
        }

        if (!skipEnergyCheck && !card.HasEnoughEnergy(this))
        {
            //Debug.Log("Not enough energy!");
            return false;
        }

        if (!card.IsTargetViewValid(target))
        {
            return false;
        }
#if !IS_DEMO
        // Prototype hack to test achievement
        GameManager.Instance.ClientCardPlayed += 5;
        Debug.Log("Updating ClientCardPlayed to:" + GameManager.Instance.ClientCardPlayed);
#endif
   
        StartCastingServerRPC(index, target.Targetable.TargetId, Hand[index].TimeInHand, skipEnergyCheck);
        return true;
    }

    [ServerRpc]
    protected void StartCastingEnergyServerRPC(int cardIndex, int energyPoolIndex)
    {
        var card = Hand[cardIndex];
        var pool = EnergyPools[energyPoolIndex];

        if (!pool.CanAddCard(card))
        {
            return;
        }

        if (card.Data.EnergyCastMultiplier == 0)
        {
            DiscardCard(card);
        }
        else
        {
            IsCasting.Value = true;
            SetDisablePlayCardsStatus(true);
            castCoroutine = StartCoroutine(EnergyCastingCoroutine(cardIndex, pool));
        }
        AddCardToPool(card, pool);
        ApplyEnergyPoolSacrificeEffects(card);

    }

    

    [ServerRpc(RequireOwnership = false)]
    private void StartCastingServerRPC(int cardIndex, string targetableId, float timeInHand, bool skipEnergyCheck)
    {
        var card = Hand[cardIndex];
        card.TimeInHand = timeInHand;

        var target = characterManager.GetTargetable(targetableId);

        if(target == null)
        {
            Debug.LogError($"Target {targetableId} wasn't found. Cancelling cast");
            return;
        }

        StartCasting(card, target, skipEnergyCheck);
    }

    public void StartCasting(Card card, ITargetable target, bool skipEnergyCheck = false)
    {
        if (card.GetCardCastingTime() == 0 || ignoreCastRequirement)
        {
            //Just play the card straight away
            DeductEnergy(card, skipEnergyCheck);
            DiscardCard(card);
            PlayCard(card.Data, target, false, card.TimeInHand);
            return;
        }
        IsCasting.Value = true;
        SetDisablePlayCardsStatus(true);
        castCoroutine = StartCoroutine(CastingCoroutine(card, target, skipEnergyCheck));
    }

    public bool CanBeTarget(TargetableView target, Card card)
    {
        if(!IsInCombat.Value || IsCasting.Value || target == null)
        {
            return false;
        }

        bool canBeTargeted = true;
        if (target.ShouldOverrideTargetAvailability(ref canBeTargeted, card))
        {
            if(!canBeTargeted)
            {
                return false;
            }
        }

        if(!card.IsTargetViewValid(target))
        {
            return false;
        }

        return true;
    }

#region Modifier
    public void AddModifier(CharacterModifier modifier)
    {
        if(Modifier != null)
        {
            Modifier.RemoveModifier();
        }

        modifier.AttachToCharacter(this);
        Modifier = modifier;

        modifier.ModifierRemoveEvent += OnModifierRemove;
        modifier.TimerUpdatedEvent += OnModifierTimerUpdated;
        AddModifierClientRPC(modifier.ID, modifier.Duration);
    }

    [ClientRpc]
    private void AddModifierClientRPC(string modifierId, float duration)
    {
        var modifier = GameDatabase.GetModifierData(modifierId);
        var characterModifier = (CharacterModifier) modifier.GetModifier();
        characterModifier.Duration = duration;
        ModifierAddedEvent?.Invoke(characterModifier);

        if (IsHost)
        {
            return;
        }
        Modifier = characterModifier;
    }

    private void OnModifierTimerUpdated()
    {
        RemainingModifierTime.Value = Modifier.Timer;
    }

    private void OnModifierRemove(Modifier modifier)
    {
        modifier.ModifierRemoveEvent -= OnModifierRemove;
        modifier.TimerUpdatedEvent -= OnModifierTimerUpdated;
        
        RemoveModifierClientRPC(modifier.Data.InternalID);
        Modifier = null;
    }

    [ClientRpc]
    private void RemoveModifierClientRPC(string modifierId)
    {
        ModifierRemovedEvent?.Invoke(Modifier);
        if (IsHost)
        {
            return;
        }
        Modifier = null;
    }
#endregion

#region Charms
    public void AddCharm(CharmData charmData)
    {
        //Get the next available Charm Slot index
        for (int i = 0; i < CharmSlots.Length; i++)
        {
            if (CharmSlots[i].IsEmpty)
            {
                AddCharm(charmData, i);
                return;
            }
        }
        Debug.Log("Couldn't find an available Charm Slot. Setting to last slot");
        AddCharm(charmData, CharmSlots.Length - 1);
    }

    public void AddCharm(CharmData charmData, int slotIndex)
    {
        if(slotIndex >= CharmSlots.Length)
        {
            Debug.Log($"{slotIndex} is bigger than the Maximum Slot Index");
            return;
        }

        if (!CharmSlots[slotIndex].IsEmpty)
        {
            CharmSlots[slotIndex].RemoveCharm();
        }

        CharmSlots[slotIndex].AddCharm(charmData);
        CharmAddedClientRPC(charmData.InternalID, slotIndex);
    }

    [ClientRpc]
    private void CharmAddedClientRPC(string charmDataId, int slotIndex)
    {
        if(IsHost)
        {
            return;
        }

        var charmData = GameDatabase.GetCharmData(charmDataId);
        CharmSlots[slotIndex].AddCharm(charmData);
    }

    private void OnCharmRemoved(CharmSlot charmSlot)
    {
        CharmRemovedClientRPC(charmSlot.Index);
    }

    [ClientRpc]
    private void CharmRemovedClientRPC(int index)
    {
        if(IsHost)
        {
            return;
        }

        CharmSlots[index].RemoveCharm();
    }
#endregion

    public float GetProjectileSpeed(float speed)
    {
        if(Modifier is ProjectileSpeedModifier m)
        {
            speed = m.GetProjectileSpeed(speed);
        }

        return speed;
    }

    public void SetTargetable(bool isTargetable)
    {
        IsTargetable.Value = isTargetable;
    }

    public void SetDisablePlayCardsStatus(bool isDisabled)
    {
        if (Modifier is FreezeModifier) return;
        DisableCardPlay.Value = isDisabled;
    }

    private void RaiseChangeCardPlayabilityEvent(bool previous, bool current)
    {
        CardPlayabilityChangedEvent?.Invoke();
    }

    private void RaiseStateChangedEvent(CharacterState previous, CharacterState current)
    {
        StateChangedEvent?.Invoke(this);
    }

    public int GetDamageToDeal(int damage)
    {
        if (Modifier is DamageSentModifier m)
        {
            damage = m.ModifyDamage(damage);
        }

        return damage;
    }

    public void OnShotProjectile(Projectile projectile)
    {
        projectile.ProjectileDestroyedEvent += OnProjectileDestroyed;
        ProjectileFiredEvent_Server?.Invoke(projectile);
        ProjectileIndex++;
    }

    private void AddSubtargetable(ITargetable targetable)
    {
        subTargetables[targetable.TargetId] = targetable;
    }

    public ITargetable TryGetSubtargetable(string targetId)
    {
        if(subTargetables.TryGetValue(targetId, out ITargetable target))
        {
            return target;
        }
        return null;
    }

    private void RemoveSubtargetable(ITargetable targetable)
    {
        subTargetables.Remove(targetable.TargetId);
    }

    private void OnProjectileDestroyed(Projectile projectile)
    {
        projectile.ProjectileDestroyedEvent -= OnProjectileDestroyed;
    }

    protected IEnumerator EnergyCastingCoroutine(int cardIndex, EnergyPool pool)
    {
        var card = Hand[cardIndex];
        RemainingCastingTime.Value = GlobalGameSettings.Settings.EnergyGenerationTimes[pool.Level.Value] * card.Data.EnergyCastMultiplier;
        RaiseStartCastingEvent(cardIndex, RemainingCastingTime.Value, CastingType.Energy, null);
        DiscardCard(cardIndex);

        while (RemainingCastingTime.Value > 0)
        {
            yield return null;
            if (IsInCombat.Value)
            {
                RemainingCastingTime.Value -= Time.deltaTime;
            }
        }
        RemainingCastingTime.Value = 0;

        IsCasting.Value = false;
        SetDisablePlayCardsStatus(false);
        castCoroutine = null;
        RaiseEndCastingEvent();
    }

    private void RaiseStartCastingEvent(int cardIndex, float timer, CastingType castingType, ITargetable target)
    {
        CardCastingStartEvent?.Invoke(cardIndex, timer, castingType);
        var card = Hand[cardIndex];

        if (IsClient)
        {
            EventBus.TriggerEvent<CastEventParams>(EventBusEnum.EventName.START_CASTING_CLIENT, new CastEventParams() { card = card, target = target });
        }

        if (IsServer)
        {
            StartCastingClientRPC(cardIndex, timer, castingType, target != null? target.TargetId : "");
        }
    }

    [ClientRpc]
    private void StartCastingClientRPC(int cardIndex, float timer, CastingType castingType, string targetId)
    {
        if(IsHost)
        {
            return;
        }
        var targetable = characterManager.GetTargetable(targetId);
        RaiseStartCastingEvent(cardIndex, timer, castingType, targetable);
    }

    private void RaiseEndCastingEvent()
    {
        CardCastingEndEvent?.Invoke();

        if(IsServer)
        {
            EndCastingClientRPC();
        }
    }

    [ClientRpc]
    private void EndCastingClientRPC()
    {
        if(IsHost)
        {
            return;
        }

        RaiseEndCastingEvent();
    }

    protected virtual void AddCardToPool(Card card, EnergyPool pool)
    {
        pool.AddCard(card);
    }

    public void AddToCastingTime(int amount)
    {
        if(!IsCasting.Value)
        {
            return;
        }

        RemainingCastingTime.Value += amount;

        if(RemainingCastingTime.Value < 0)
        {
            RemainingCastingTime.Value = 0;
        }
    }

    public void MultiplyCastingTime(float multiplier)
    {
        if (!IsCasting.Value)
        {
            return;
        }

        RemainingCastingTime.Value *= multiplier;
    }

    private IEnumerator CastingCoroutine(Card card, ITargetable target, bool skipEnergyCheck)
    {
        RemainingCastingTime.Value = card.GetCardCastingTime();
        RaiseStartCastingEvent(card.HandIndex, RemainingCastingTime.Value, CastingType.Spell, null);
        DiscardCard(card.HandIndex);
        DeductEnergy(card, skipEnergyCheck);

        ApplyStartCastCardEffects(card, target);

        while(RemainingCastingTime.Value > 0)
        {
            yield return null;
            if(IsInCombat.Value && Modifier is not FreezeModifier)
            {
                RemainingCastingTime.Value -= Time.deltaTime;
            }
        }
        RemainingCastingTime.Value = 0;

        PlayCard(card.Data, target, false, card.TimeInHand);

        castCoroutine = null;
        RaiseEndCastingEvent();
    }

    protected virtual void ApplyEnergyPoolSacrificeEffects(Card card)
    {
        card.ApplyEnergyPoolSacrificeEffects(this);
    }

    protected virtual void ApplyStartCastCardEffects(Card card, ITargetable target)
    {
        card.ApplyStartCastEffects(this, target);
    }

    private void UpdateCastSpellHistory(CardHistoryRecord record)
    {
        CastCardHistory.Add(record);
        if (IsServer)
        {
            UpdateCastSpellHistoryClientRPC(record);
        }
    }

    [ClientRpc]
    private void UpdateCastSpellHistoryClientRPC(CardHistoryRecord record)
    {
        if(IsHost)
        {
            return;
        }

        UpdateCastSpellHistory(record);
    }
    
    public void PlayCard(CardData cardData, ITargetable target, bool ignoreSpellHistory = false, float timeInHand = 0)
    {
        //Create copy of card so that original card is not affected by adding additional effects
        var card = new Card(cardData)
        {
            TimeInHand = timeInHand
        };
        if (!ignoreSpellHistory && card.Data.CardType == CardTypes.Spell)
        {
            UpdateCastSpellHistory(new CardHistoryRecord(DateTime.Now, card));
        }

        var selfTarget = target.TargetId == ((ITargetable)this).TargetId || target.TargetId.Contains(Card.CARD_TAG);

        card.SetOwner(this);
        

        if (selfTarget)
        {
            projectileSpawnPointParent.eulerAngles = Vector3.zero;
        }
        else
        {
            Vector3 relPos = target.Position - transform.position;
            Quaternion newRotation = Quaternion.LookRotation(relPos, Vector3.up);
            projectileSpawnPointParent.rotation = Quaternion.Euler(0f, newRotation.eulerAngles.y, 0f);
        }

        CardPlayedEvent_Server?.Invoke(card);
        card.PlayCard(this, target);

        CardPlayedClientRPC(cardData.InternalID, selfTarget, target.TargetId);

        IsCasting.Value = false;
        SetDisablePlayCardsStatus(false);
    }

    [ClientRpc]
    private void CardPlayedClientRPC(string cardId, bool selfTarget, string targetId)
    {
        var target = characterManager.GetTargetable(targetId);
        CardPlayedEvent_Client?.Invoke(cardId, selfTarget, target);
    }

    public void BatchDiscardCard(List<int> handIndexList, Card sourceCard, float slowdown = 1f)
    {
        var successCount = handIndexList.Count(cardIndex => DiscardCard(cardIndex, slowdown));
        CardBatchDiscardedEvent?.Invoke(sourceCard, successCount);
    }
    public void BatchDrawCard(int drawCount)
    {
        for (var i = 0; i < drawCount; i++)
        {
            DrawCard();
        }
    }
    
    public bool DiscardCard(int cardIndex, float slowdown = 1)
    {
        if(cardIndex < 0 || cardIndex >= Hand.Length)
        {
            Debug.LogError("Invalid Card Index");
            return false;
        }

        if (Hand[cardIndex] == null)
        {
            Debug.LogError("Card Index is empty");
            return false;
        }

        if (IsServer)
        {
            InternalDiscardCard(cardIndex, slowdown);
            TryStartDrawCoroutine();
        }
        else
        {
            DiscardCardServerRpc(cardIndex, slowdown);
        }

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void DiscardCardServerRpc(int cardIndex, float slowdown)
    {
        DiscardCard(cardIndex, slowdown);
    }

    protected virtual void InternalDiscardCard(int cardIndex, float slowdown = 1)
    {
        var card = Hand[cardIndex];
        Hand[cardIndex] = null;
        HandSize--;
        HandChangedEvent?.Invoke();
        card.SetHandIndex(-1);
        RemoveSubtargetable(card);
        CardDiscardedEvent?.Invoke(card, cardIndex, slowdown);

        if (IsServer)
        {
            discardPile.Add(card);
            DiscardPileSize.Value = discardPile.Count;
            DiscardCardClientRPC(cardIndex, slowdown);
        }
        
    }

    [ClientRpc]
    private void DiscardCardClientRPC(int cardIndex, float slowdown = 1)
    {
        if(IsHost)
        {
            return;
        }
        InternalDiscardCard(cardIndex, slowdown);
    }

    public void DiscardCard(Card card)
    {
        for (int i = 0; i < Hand.Length; i++)
        {
            if (Hand[i] == card)
            {
                DiscardCard(i);
                return;
            }
        }
    }

    public void CancelCasting()
    {
        if(!IsCasting.Value)
        {
            return;
        }

        StopCoroutine(castCoroutine);
        IsCasting.Value = false;
        SetDisablePlayCardsStatus(false);
        RaiseEndCastingEvent();
    }

    [ServerRpc]
    public void CancelCastingServerRPC()
    {
        CancelCasting();
    }

    protected virtual void TryStartDrawCoroutine()
    {
        if (drawCoroutine != null || !CanDrawCards())
        {
            return;
        }

        CardDrawStartEvent?.Invoke();
        StartCardDrawClientRPC();
        drawCoroutine = StartCoroutine(DrawCoroutine());
    }

    [ClientRpc]
    protected void StartCardDrawClientRPC()
    {
        if(IsHost && PlayerData.Id == NetworkManager.LocalClientId)
        {
            return;
        }

        CardDrawStartEvent?.Invoke();
    }

    private IEnumerator DrawCoroutine()
    {
        RemainingDrawTime.Value = Data.CardDrawTimer * roundManager.GetRoundDrawTimerMultiplier();
        while (RemainingDrawTime.Value > 0)
        {
            if (IsInCombat.Value)
            {
                RemainingDrawTime.Value -= Time.deltaTime;
            }
            yield return null;
        }
        ClearDrawCoroutine();
        DrawCard();
        CardDrawEndEvent?.Invoke();
        TryStartDrawCoroutine();
    }

    [ClientRpc]
    protected void EndCardDrawClientRPC()
    {
        if (IsHost && PlayerData.Id == NetworkManager.LocalClientId)
        {
            return;
        }

        CardDrawEndEvent?.Invoke();
    }

    private void ClearDrawCoroutine()
    {
        StopCoroutine(drawCoroutine);
        drawCoroutine = null;
    }

    private bool CanDrawCards()
    {
        if (characterManager.NumberOfTeamsAlive == 1 || IsDead)
        {
            return false;
        }
        return HandSize < Hand.Length;
    }

    private void DeductEnergy(Card card, bool skipEnergyCheck)
    {
        if (skipEnergyCheck) return;
        
        var energyCosts = card.Data.EnergyCost;
        foreach(var cost in energyCosts)
        {
            int totalCostByColor = cost.cost;
            foreach (var pool in EnergyPools)
            {
                if (pool.Color.Value == cost.color)
                {
                    if (pool.Energy.Value >= totalCostByColor)
                    {
                        pool.DeductEnergy(totalCostByColor);
                        totalCostByColor = 0;
                    }
                    else
                    {
                        totalCostByColor -= pool.Energy.Value;
                        pool.DeductEnergy(pool.Energy.Value);
                    }
                }
            }
        }
    }

    private void UpdateDamageHistory(int damage, Card card)
    {
        if (card.Owner)
        {
            card.Owner.AddDealtDamageHistory(new DamageHistoryRecord(DateTime.Now, card, card.Owner, this, damage));
        }
        AddReceivedDamageHistory(new DamageHistoryRecord(DateTime.Now, card, card.Owner, this, damage));
    }

    public void AddDealtDamageHistory(DamageHistoryRecord record)
    {
        DealtDamageHistory.Add(record);
        if (IsServer)
        {
            AddDealtDamageHistoryClientRpc(record);
        }
    }

    [ClientRpc]
    private void AddDealtDamageHistoryClientRpc(DamageHistoryRecord record)
    {
        if(IsHost)
        {
            return;
        }

        AddDealtDamageHistory(record);
    }

    public void AddReceivedDamageHistory(DamageHistoryRecord record)
    {
        ReceivedDamageHistory.Add(record);

        if (IsServer)
        {
            AddReceivedDamageHistoryClientRpc(record);
        }
    }

    [ClientRpc]
    private void AddReceivedDamageHistoryClientRpc(DamageHistoryRecord record)
    {
        if(IsHost)
        {
            return;
        }

        AddReceivedDamageHistory(record);
    }

    public int DealDamage(int damage, Card card)
    {
        if(IsDead)
        {
            return 0;
        }

        if(Modifier is DamageReceivedModifier m)
        {
            damage = m.ModifyDamage(damage);
        }

        if (card != null)
        {
            UpdateDamageHistory(damage, card);    
        }
        //Debug.Log($"{damage} Damage dealt to {Data.Name}");
        InternalSetHealth(Health.Value - damage, card);

        return damage;
    }

    private void DeathTickApplied(int damage)
    {
        DealDamage(damage, null);
    }

    private AdditionalDamageModifier GetAdditionalDamageModifier()
    {
        return Modifier is AdditionalDamageModifier m ? m : null;
    }

    public int GetAdditionalDamage()
    {
        var modifier = GetAdditionalDamageModifier();
        return modifier?.GetDamageAmount() ?? 0;
    }

    public void DelayHeal(int healAmount, float delay)
    {
        StartCoroutine(HealCoroutine(healAmount, delay));
    }

    private IEnumerator HealCoroutine(int healAmount, float delay)
    {
        yield return new WaitForSeconds(delay);
        Heal(healAmount);
    }
    public void Heal(int healAmount)
     {
        if(IsDead)
        {
            return;
        }

        InternalSetHealth(Health.Value + healAmount);
    }

    protected virtual void InternalSetHealth(int health, Card card = null)
    {
        var healthChange = health - Health.Value;
        
        // Can only alter health when alive
        if (State.Value == CharacterState.Alive)
        {
            Health.Value = Mathf.Clamp(health, 0, MaxHealth.Value);
            if (card != null)
            {
                HealthChangedByCardEvent?.Invoke(this, healthChange, card);
            }

            if (Health.Value <= 0)
            {
                MarkAsDown(card);
                return;   
            }  
        }
        
        if (State.Value == CharacterState.Downed)
        {
            UpdateDeathTimer(healthChange);
        }
    }

    private void MarkAsDown(Card card = null)
    {
        State.Value = CharacterState.Downed;
        DeathTimer.Value = MaxDeathTimer;
        deathTimerCoroutine = StartCoroutine(StartDeathTimer(card));
    }

    private IEnumerator StartDeathTimer(Card card)
    {
        while (DeathTimer.Value > 0)
        {
            yield return new WaitForSeconds(1);
            UpdateDeathTimer(-1);
        }
        Die(card);
        ClearDeathTimer();
    }

    private void UpdateDeathTimer(int healthChange)
    {
        if (State.Value != CharacterState.Downed || !IsInCombat.Value) return;
        
        DeathTimer.Value += healthChange;
        if (DeathTimer.Value > MaxDeathTimer)
        {
            DeathTimer.Value = MaxDeathTimer;
        }
        else if (DeathTimer.Value <= 0)
        {
            DeathTimer.Value = 0;
        }
    }

    public void Revive(int health)
    {
        if (State.Value != CharacterState.Downed) return;
        
        State.Value = CharacterState.Alive;
        DeathTimer.Value = 0;
        ClearDeathTimer();
        InternalSetHealth(health);
    }

    private void ClearDeathTimer()
    {
        if (deathTimerCoroutine == null) return;
        
        StopCoroutine(deathTimerCoroutine);
        deathTimerCoroutine = null;
    }

    public void Kill(Card sourceCard = null)
    {
        if(IsDead)
        {
            return;
        }

        Health.Value = 0;
        Die(sourceCard);
    }

    private void Die(Card sourceCard = null)
    {
        State.Value = CharacterState.Dead;

        if (IsCasting.Value)
        {
            CancelCasting();
        }

        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
        }

        foreach (var card in Hand)
        {
            if (card != null)
            {
                DiscardCard(card);
            }
        }

        if (DraftPackBacklog.Count > 0)
        {
            //If there's packs currently on the dead character, pick the top cards from them just to send them forward
            List<DraftPack> draftedPacks = new List<DraftPack>(DraftPackBacklog);

            foreach (var pack in draftedPacks)
            {
                PickFromDraftPack(pack.GetFirstItemId(), pack, out Card card, out Charm charm);
            }
        }
        DraftPackBacklog.Clear();
        cardEffectsManager.DestroyProjectiles(this);
        UnsubscribeEvents();

        Character killer = null;
        if(sourceCard == null)
        {
            //Get last character that damaged this
            if (ReceivedDamageHistory.Count > 0)
            {
                foreach(var c in characterManager.Characters)
                {
                    if(c.Index == ReceivedDamageHistory[ReceivedDamageHistory.Count - 1].sourceCharacterIndex)
                    {
                        killer = c;
                        break;
                    }
                }
            }
        }
        else
        {
            killer = sourceCard.Owner;
        }

        
        RaiseDeathEvent(sourceCard, killer);
    }

    private void RaiseDeathEvent(Card killCard, Character killer)
    {
        DeathEvent?.Invoke(killCard, killer);
        
        if(IsServer)
        {
            DeathClientRPC(killCard != null ? killCard.Data.InternalID : "",
                killer != null ? killer.Index : -1);
        }
    }

    [ClientRpc]
    private void DeathClientRPC(string cardId, int killerIndex)
    {
        if(IsHost)
        {
            return;
        }
        Character killer = null;
        if(killerIndex >= 0)
        {
            foreach(var c in characterManager.Characters)
            {
                if(c.Index == killerIndex)
                {
                    killer = c;
                    break;
                }
            }
        }
        Card card = null;
        if(!string.IsNullOrEmpty(cardId))
        {
            card = new Card(GameDatabase.GetCardData(cardId));
        }
        RaiseDeathEvent(card, killer);
    }

#region Debug
    public void DebugMaxMana()
    {
        if (Debug.isDebugBuild)
        {
            DebugMaxManaServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugMaxManaServerRPC()
    {
#if UNITY_EDITOR        
        for (int i = 0; i < EnergyPools.Length; i++)
        {
            EnergyPool p = EnergyPools[i];
            if (p.IsEmpty)
            {
                p.Color.Value = i % 2 == 0 ? CardColors.Black : CardColors.White;
            }
            p.DebugMaxMana();
        }
#endif        
    }

    public void DebugAddCardToHand(string cardId)
    {
        if(Debug.isDebugBuild)
        {
            DebugAddCardToHandServerRPC(cardId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugAddCardToHandServerRPC(string cardId)
    {
        var card = new Card(GameDatabase.GetCardData(cardId));
        card.SetOwner(this);
        if (HandSize < Hand.Length)
        {
            AddCardToHand(card);
        }
        else
        {
            AddCardToTopDeck(card);
        }
    }

    public void DebugDiscardHand()
    {
        if (Debug.isDebugBuild)
        {
            DebugDiscardHandServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugDiscardHandServerRPC()
    {
        for (int i = 0; i < Hand.Length; i++)
        {
            DiscardCard(i);
        }
    }
#endregion

        public void SetFirst()
    {

    }

    public void SetLast()
    {

    }

    public void AddCardToTopDeck(Card card, Vector3? animationPosition = null)
    {
        deck.AddCardToTop(card);
        card.SetOwner(this);
        DeckSize.Value = deck.DeckSize;

        if(animationPosition.HasValue)
        {
            CardAddToDeckWithPositionEvent?.Invoke(card, animationPosition.Value);
            AddCardToTopDeckWithPositionClientRpc(card.Data.InternalID, animationPosition.Value);
        }
    }
    
    public void AddCardToTopDeckWithPosition(Card card, Vector3 worldPosition)
    {
        EventBus.TriggerEvent<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_DECK_WITH_POSITION_SERVER, new CardAddedData()
        {
            card = card,
            currentPosition = worldPosition,
            targetCharacter = this
        });
    }

    [ClientRpc]
    private void AddCardToTopDeckWithPositionClientRpc(string cardId, Vector3 worldPosition)
    {
        if(IsHost)
        {
            return;
        }
        var card = new Card(GameDatabase.GetCardData(cardId));
        CardAddToDeckWithPositionEvent?.Invoke(card, worldPosition);
    }

#region Draft
    public void AddDraftCardToHand(string cardId, string draftPackId, Vector3 position)
    {
        var card = PickCardFromDraftPack(cardId, draftPackId);
        if(card == null)
        {
            return;
        }
        card.SetOwner(this);
        AddCardToHand(card, position);
    }

    public void AddDraftCardToDeck(string cardId, string draftPackId, Vector2 position)
    {
        var card = PickCardFromDraftPack(cardId, draftPackId);
        if (card == null)
        {
            return;
        }
        AddCardToTopDeck(card, position);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddDraftCardToHandServerRPC(string cardId, string draftPackId, Vector2 position)
    {
        AddDraftCardToHand(cardId, draftPackId, position);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddDraftCardToDeckServerRPC(string cardId, string draftPackId, Vector2 position)
    {
        AddDraftCardToDeck(cardId, draftPackId, position);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddDraftCharmToSlotServerRPC(string charmId, int slot, string draftPackId)
    {
        var charm = PickCharmFromDraftPack(charmId, draftPackId);
        if(charm == null)
        {
            return;
        }
        AddCharm(charm.CharmData, slot);
    }

    private Card PickCardFromDraftPack(string cardId, string draftPackId)
    {
        PickFromDraftPack(cardId, draftPackId, out Card card, out Charm charm);

        if(card == null)
        {
            Debug.LogError($"Couldn't find Card of id {cardId} in draft pack!");
            return null;
        }
        return card;
    }

    private Charm PickCharmFromDraftPack(string charmId, string draftPackId)
    {
        PickFromDraftPack(charmId, draftPackId, out Card card, out Charm charm);

        if (charm == null)
        {
            Debug.LogError($"Couldn't find Charm of id {charmId} in draft pack!");
            return null;
        }
        return charm;
    }

    public void PickFromDraftPack(string pickedId, string draftPackId, out Card card, out Charm charm)
    {
        if(!string.IsNullOrEmpty(draftPackId) && DraftPackBacklog.Count > 0)
        {
            DraftPack draftPack = null;
            foreach (var pack in DraftPackBacklog)
            {
                if (pack.ID == draftPackId)
                {
                    draftPack = pack;
                    break;
                }
            }

            if (draftPack != null)
            {
                PickFromDraftPack(pickedId, draftPack, out card, out charm);
                return;
            }
        }

        card = null;
        charm = null;
        Debug.LogError($"Couldn't find Draft Pack with Id {draftPackId}. DraftBacklogCount {DraftPackBacklog.Count}");
    }

    public void ForceDraftPacksCompletion()
    {
        foreach (DraftPack draftPack in DraftPackBacklog)
        {
            draftPack.Complete();
            DraftPackCompleteClientRPC(draftPack.GetData(CharacterView.transform.position));
        }
        DraftPackBacklog.Clear();
    }

    public void PickFromDraftPack(string pickedId, DraftPack draftPack, out Card card, out Charm charm)
    {
        draftPack.Pick(pickedId, out card, out charm);

        if (draftPack.IsComplete)
        {
            DraftPackCompleteClientRPC(draftPack.GetData(transform.position));
        }
        else
        {
            SendDraftPackToNextOwner(draftPack, PickNextDraftPackOwner());
        }

        DraftPackBacklog.Remove(draftPack);
    }

    [ClientRpc]
    private void DraftPackCompleteClientRPC(DraftPackData draftPackData)
    {
        EventBus.TriggerEvent<DraftPackData>(EventBusEnum.EventName.DRAFT_PACK_COMPLETE_CLIENT, draftPackData);
    }

    protected virtual void SendDraftPackToNextOwner(DraftPack draftPack, Character nextOwner)
    {
        if (draftPack.IsComplete)
        {
            return;
        }

        draftPack.PassPack(nextOwner);
    }

    protected Character PickNextDraftPackOwner()
    {
        var characters = characterManager.Characters;
        int currentIndex = characters.IndexOf(this);
        for (int i = currentIndex + 1; i < characters.Count; i++)
        {
            if (!characters[i].IsDead)
            {
                return characters[i];
            }
        }

        for (int i = 0; i < currentIndex; i++)
        {
            if (!characters[i].IsDead)
            {
                return characters[i];
            }
        }

        Debug.LogError("Couldn't find proper Next Owner. Getting first non-Dead non-Player character");
        return characters.First(c => !c.IsDead && c != this);
    }
#endregion

    public int GetSpellAmountWithinTimeRange(DateTime endTime, int duration)
    {
        var thresholdTime = endTime.AddSeconds(-duration);
        return CastCardHistory.Count(record => record.time > thresholdTime);
    }

    public int GetDealtDamageWithinTimeRange(DateTime endTime, int duration)
    {
        var thresholdTime = endTime.AddSeconds(-duration);
        return DealtDamageHistory
            .FindAll(record => record.time > thresholdTime)
            .Sum(item => item.damage);
    }

    public int GetReceivedDamageWithinTimeRange(DateTime endTime, int duration)
    {
        var thresholdTime = endTime.AddSeconds(-duration);
        return ReceivedDamageHistory
            .FindAll(record => record.time > thresholdTime)
            .Sum(item => item.damage);
    }

    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        if (IsDead)
        {
            canBeTargeted = false;
            return true;
        }
        if (!IsTargetable.Value)
        {
            canBeTargeted = false;
            return true;
        }
        return false;
    }

    public void ChangeHandSize(int amount)
    {
        int newHandSize = Hand.Length + amount;
        if(newHandSize > GlobalGameSettings.Settings.MaxHandSize)
        {
            newHandSize = GlobalGameSettings.Settings.MaxHandSize;
        }
        if(newHandSize < GlobalGameSettings.Settings.StartHandSize)
        {
            newHandSize = GlobalGameSettings.Settings.StartHandSize;
        }

        if(newHandSize == Hand.Length)
        {
            return;
        }

        var newHand = new Card[newHandSize];
        for(int i = 0; i < Hand.Length; i++)
        {
            newHand[i] = Hand[i];
        }

        if (IsServer)
        {
            if (newHandSize < Hand.Length)
            {
                for (int i = newHandSize; i < Hand.Length; i++)
                {
                    DiscardCard(i);
                }
            }
        }
        Hand = newHand;
        HandChangedEvent?.Invoke();

        if (IsServer)
        {
            TryStartDrawCoroutine();
            ChangeHandSizeClientRPC(amount);
        }
    }

    [ClientRpc]
    private void ChangeHandSizeClientRPC(int amount)
    {
        if(IsHost)
        {
            return;
        }
        ChangeHandSize(amount);
    }

    public bool SendCardToAlly(int handIndex)
    {
        if (IsCasting.Value)
        {
            return false;
        }

        if (!IsInCombat.Value)
        {
            return false;
        }

        if(State.Value == CharacterState.Dead)
        {
            return false;
        }

        SendCardToAllyServerRPC(handIndex);
        return true;
    }

    [ServerRpc]
    public void SendCardToAllyServerRPC(int handIndex)
    {
        var card = Hand[handIndex];
        if (card == null)
        {
            Debug.LogError("Cannot send an empty card to Ally");
            return;
        }

        var allies = characterManager.GetAllAllies(this);
        if (allies.Count == 0)
        {
            return;
        }

        if(ignoreCastRequirement)
        {
            SendCardToAlly(allies[0], card);
            return;
        }

        IsCasting.Value = true;
        SetDisablePlayCardsStatus(true);
        castCoroutine = StartCoroutine(CastingSendCardCoroutine(allies[0], handIndex));
    }

    private IEnumerator CastingSendCardCoroutine(Character ally, int handIndex)
    {
        RemainingCastingTime.Value = GlobalGameSettings.Settings.SendCardCastingDuration;
        RaiseStartCastingEvent(handIndex, RemainingCastingTime.Value, CastingType.CardSend, null);
        var card = Hand[handIndex];
        DestroyCard(handIndex);
        while (RemainingCastingTime.Value > 0)
        {
            yield return null;
            if (IsInCombat.Value)
            {
                RemainingCastingTime.Value -= Time.deltaTime;
            }
        }
        RemainingCastingTime.Value = 0;

        IsCasting.Value = false;
        SetDisablePlayCardsStatus(false);
        castCoroutine = null;
        RaiseEndCastingEvent();

        SendCardToAlly(ally, card);
    }

    private void SendCardToAlly(Character ally, Card card)
    {
        if(card == null)
        {
            Debug.LogError($"Card was empty. Cannot send card to ally.");
            return;
        }

        if (ally.HandSize < ally.Hand.Length)
        {
            ally.AddCardToHandWithPosition(card, transform.position);
        }
        else
        {
            ally.AddCardToTopDeckWithPosition(card, transform.position);
        }
    }

    public void DestroyCard(int handIndex)
    {
        HandSize--;
        HandChangedEvent?.Invoke();
        CardDestroyedEvent?.Invoke(handIndex);
        Hand[handIndex] = null;

        if (IsServer)
        {
            TryStartDrawCoroutine();
            DestroyCardClientRPC(handIndex);
        }
    }

    [ClientRpc]
    private void DestroyCardClientRPC(int handIndex)
    {
        if(IsHost)
        {
            return;
        }

        DestroyCard(handIndex);
    }

    public void ReviveWithCharm(int health, float wait, CharmData charmData)
    {
        if (IsServer)
        {
            StartCoroutine(ReviveCoroutine(health, wait, charmData));    
        }
    }

    private IEnumerator ReviveCoroutine(int health, float wait, CharmData charmData)
    {
        yield return new WaitForSeconds(wait);
        Revive(health);
        RemoveCharm(charmData);
    }

    public void RemoveCharm(CharmData charmData)
    {
        foreach (var charmSlot in CharmSlots)
        {
            if (charmSlot.Charm != null && charmSlot.Charm.CharmData == charmData)
            {
                charmSlot.RemoveCharm();
                return;
            }
        }
    }

    public void RemoveAutoHealAndDamageCoroutine()
    {
        if (healAndDamageCoroutine == null) return;
        StopCoroutine(healAndDamageCoroutine);
        healAndDamageCoroutine = null;
    }

    public void HealAllyDealDamageToSelf(int healthDiff, int healAmount, int damageAmount, float wait)
    {
        healAndDamageCoroutine = StartCoroutine(HealAndDamageCoroutine(healthDiff, healAmount, damageAmount, wait));
    }
    
    private IEnumerator HealAndDamageCoroutine(int healthDiff, int healAmount, int damageAmount, float wait)
    {
        while (!IsDead)
        {
            if (IsInCombat.Value)
            {
                var allies = characterManager.GetAllAllies(this);
                if (allies.Count == 0) yield return null;
                var targetAllies = allies
                    .Where(ally => !ally.IsDead && ally.Health.Value < Health.Value - healthDiff)
                    .ToList();
                
                if (targetAllies.Count > 0)
                {
                    AutoHealAndDamage(targetAllies, healAmount, damageAmount);    
                }    
            }
            
            yield return new WaitForSeconds(wait);
        }

        RemoveAutoHealAndDamageCoroutine();
    }

    private void AutoHealAndDamage(List<Character> allies, int healAmount, int damageAmount)
    {
        if (allies.Count == 0) return;
        foreach (var ally in allies)
        {
            ally.Heal(healAmount);
        }
        DealDamage(damageAmount, null);
    }

    public void SetIgnoreCasting(bool ignoreCast)
    {
        ignoreCastRequirement = ignoreCast;
    }

    public void AssignNemesis(Character character)
    {
        Nemesis = character;
        NemesisIndex.Value = character.Index;
        Nemesis.DeathEvent += OnNemesisDied;
    }

    private void OnNemesisDied(Card sourceCard, Character killer)
    {
        if(killer == this)
        {
            //TODO: Add RP gain
            Debug.Log($"{PlayerData.DisplayName} successfully killed their Nemesis {Nemesis.PlayerData.DisplayName}");
            NemesisKilledClientRPC();
        }
    }

    [ClientRpc]
    private void NemesisKilledClientRPC()
    {
        EventBus.TriggerEvent<Character>(EventBusEnum.EventName.CHARACTER_KILLED_NEMESIS_CLIENT, this);
    }
}
