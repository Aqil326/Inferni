using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Netcode;

public struct DraftPickData
{
    public int timer;
    public int totalDuration;
}

public class RoundManager : BaseManager
{
    [Serializable]
    public class Round
    {
        public enum RoundType { Combat, Draft };
        public RoundType roundType;
        public float drawTimeMultiplier = 1;
        public int secondsLong;
    }

    public class DraftPickTimer
    {
        public int pickNumber;
        public int timer;
        public Character character;

        public int PickDuration => pickDurations[pickNumber];
        public ClientRpcParams ClientRpcParams { get; private set; }

        private List<int> pickDurations;

        public DraftPickTimer(Character character, List<int> pickDurations)
        {
            this.character = character;
            this.pickDurations = pickDurations;
            pickNumber = 0;
            timer = pickDurations[pickNumber];

            ClientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { character.PlayerData.Id }
                }
            };
        }

        public void IncreasePick()
        {
            pickNumber++;
            timer = PickDuration;
        }
    }

    public NetworkVariable<int> RoundIndex = new NetworkVariable<int>();
    public NetworkVariable<int> CombatRoundTimer = new NetworkVariable<int>();
    public NetworkVariable<int> DraftEndTimer = new NetworkVariable<int>();
    public NetworkVariable<bool> IsTimerPaused = new NetworkVariable<bool>();
    public List<int> draftPickDurations;
    public List<Round> rounds = new List<Round>();
    public List<DraftPickTimer> draftPickTimers = new List<DraftPickTimer>();

    public Round CurrentRound => rounds[RoundIndex.Value];
    public bool IsLastRound => RoundIndex.Value == rounds.Count - 1;
    public List<DraftPack> DraftPacks { get; private set; }
    public bool IsDeathTick { get; private set; }

    readonly float tickTime = 1;
    private int tickCount;
    private NakamaNetworkManager NakamaNetworkManager;
    private CharacterManager characterManager;
    private int playerMessageCount;
    private Coroutine tickCoroutine;
    private int draftRoundCount;
    private float deathTickTimer;
    private int deathTickCount;
    
    public List<List<Card>> CardsList;
    public List<List<Charm>> CharmsList;

    [SerializeField]
    SoundManager.Sound combatBeginSound;
    [SerializeField]
    SoundManager.Sound draftBeginSound;
    [SerializeField]
    SoundManager.Sound arenaAmbienceSound;
    [SerializeField]
    SoundManager.Sound tickSound;
    [SerializeField]
    SoundManager.Sound tockSound;
    private bool isTick = true;
    [SerializeField]
    private int tickDraftTimeRemaining = 10;
    [SerializeField]
    private int tickCombatTimeRemaining = 10;

#if UNITY_SERVER
    private bool IsServer = true;
#else
    private bool IsServer = false;
#endif
#if UNITY_EDITOR
    private bool isEditor = true;
#else
    private bool isEditor = false;
#endif

    public override void Init()
    {
        base.Init();
        characterManager = GameManager.GetManager<CharacterManager>();
    }

    public void StartGame()
    {
        RoundIndex.Value = 0;
        StartRound();
    }

    public float GetRoundDrawTimerMultiplier()
    {
        return CurrentRound.drawTimeMultiplier;
    }

    private void  StartRound()
    {
        switch (CurrentRound.roundType)
        {
            case Round.RoundType.Combat:
                StartRoundCombat();
                break;
            case Round.RoundType.Draft:
                StartRoundDraft();
                break;
            default:
                Debug.LogWarning("Undefined round type");
                break;
        }

        //Is first round
        if (RoundIndex.Value == 0)
        {
            if (tickCoroutine == null)
            {
                tickCoroutine = StartCoroutine(TickTimer());
            }
        }
    }

    private IEnumerator TickTimer()
    {
        while (true)
        {
            if (IsTimerPaused.Value || characterManager.NumberOfTeamsAlive == 1)
            {
                yield return null;
            }
            else
            {
                tickCount++;
                Tick();
                yield return new WaitForSeconds(tickTime);
            }
        } 
    }

    private void Tick()
    {
        if(IsDeathTick)
        {
            deathTickTimer--;

            if(deathTickTimer <= 0)
            {
                int damageAmount = GlobalGameSettings.Settings.DeathTickDamageAmount + (int)(GlobalGameSettings.Settings.DeathTickIncreasePerTick * deathTickCount);
                EventBus.TriggerEvent(EventBusEnum.EventName.DEATH_TICK_APPLIED_SERVER, damageAmount);
                deathTickCount++;
                deathTickTimer = GlobalGameSettings.Settings.DeathTickIncreaseDuration;
            }

            return;
        }

        if(CurrentRound.roundType == Round.RoundType.Draft)
        {
            if (DraftEndTimer.Value > 0)
            {
                DraftEndTimer.Value--;

                if(DraftEndTimer.Value == 0)
                {
                    RoundIndex.Value++;
                    StartRound();
                }
            }
            else
            {
                var timersToForcePick = new List<DraftPickTimer>();
                foreach (var draftTimer in draftPickTimers)
                {
                    if (draftTimer.character.DraftPackBacklog.Count > 0)
                    {
                        draftTimer.timer--;
                        UpdateDraftTimerClientRPC(draftTimer.timer, draftTimer.PickDuration, draftTimer.ClientRpcParams);

                        if (draftTimer.timer <= 0)
                        {
                            timersToForcePick.Add(draftTimer);
                        }
                    }
                }

                if (timersToForcePick.Count > 0)
                {
                    foreach (var timer in timersToForcePick)
                    {
                        //Force card pick
                        var character = timer.character;
                        var pack = timer.character.DraftPackBacklog[0];
                        character.PickFromDraftPack(pack.GetFirstItemId(), pack, out Card card, out Charm charm);
                        if (card != null)
                        {
                            character.AddCardToTopDeck(card, Vector3.zero);
                        }
                        else if (charm != null)
                        {
                            character.AddCharm(charm.CharmData);
                        }
                    }
                }
            }
        }
        else if (CurrentRound.roundType == Round.RoundType.Combat)
        {
            CombatRoundTimer.Value--;

            if (CombatRoundTimer.Value <= 0)
            {
                if (IsLastRound)
                {
                    IsDeathTick = true;
                    deathTickTimer = GlobalGameSettings.Settings.DeathTickIncreaseDuration;
                    StartDeathTickClientRPC();
                }
                else
                {
                    EndPreviousRound();
                    RoundIndex.Value++;
                    StartRound();
                }
            }
        }
    }

    [ClientRpc]
    private void UpdateDraftTimerClientRPC(int timer, int totalDuration, ClientRpcParams clientRpcParams = default)
    {
        EventBus.TriggerEvent<DraftPickData>(EventBusEnum.EventName.DRAFT_TIMER_UPDATED_CLIENT, new DraftPickData() { timer = timer, totalDuration = totalDuration});
        if (timer <= tickDraftTimeRemaining) PlayTickSound();
    }

    [ClientRpc]
    private void StartDeathTickClientRPC()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.START_DEATH_TICK_CLIENT);
        if(IsHost)
        {
            return;
        }
        IsDeathTick = true;
    }

    private void StartRoundCombat()
    {
        CombatRoundTimer.Value = rounds[RoundIndex.Value].secondsLong;

        EventBus.TriggerEvent(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER);
        StartCombatClientRPC();
        
    }

    [ClientRpc]
    private void StartCombatClientRPC()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT);
        combatBeginSound.Play(false);
    }

    [ClientRpc]
    private void EndCombatClientRPC()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.END_ROUND_COMBAT_CLIENT);
    }

    private void StartRoundDraft()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.START_ROUND_DRAFT_SERVER);

        foreach(var character in characterManager.Characters)
        {
            character.ChangeHandSize(GlobalGameSettings.Settings.HandSizeIncreasePerDraft);
        }

        StartDraftClientRPC();

        //Create draft packs list
        DraftPacks = new List<DraftPack>();

        //Get all the alive characters
        List<Character> characters = new List<Character>();
        foreach (Character character in characterManager.Characters)
        {
            if (!character.IsDead)
            {
                characters.Add(character);
                draftPickTimers.Add(new DraftPickTimer(character, draftPickDurations));
            }
        }
        
        for (int i = 0; i < characters.Count; i++)
        {
            InitRandomDraftPackPools(draftRoundCount);
            DraftPack newPack = AssignDraftPack(characters[i]);
            DraftPacks.Add(newPack);
            newPack.DraftPackPassedEvent += OnDraftPackPass;
            newPack.DraftPackCompleteEvent += OnDraftPackComplete;
            characters[i].ReceiveDraftPack(newPack);
        }
        draftRoundCount++;
    }

    [ClientRpc]
    private void StartDraftClientRPC()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT);

        draftBeginSound.Play(false);
    }

    [ClientRpc]
    private void EndDraftClientRPC()
    {
        EventBus.TriggerEvent(EventBusEnum.EventName.END_ROUND_DRAFT_CLIENT);
    }

    private void EndPreviousRound()
    { 
        switch(CurrentRound.roundType)
        {
            case Round.RoundType.Combat:
                EndCombatClientRPC();
                EventBus.TriggerEvent(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER);
                break;
            case Round.RoundType.Draft:
                EndDraftClientRPC();
                EventBus.TriggerEvent(EventBusEnum.EventName.END_ROUND_DRAFT_SERVER);
                draftPickTimers.Clear();
                DraftEndTimer.Value = GlobalGameSettings.Settings.DraftToCombatStartDelay;
                break;
            default:
                Debug.LogWarning("Undefined round type");
                break;
        }
    }

    private void OnDraftPackPass(DraftPack draftPack)
    {
        foreach(var timer in draftPickTimers)
        {
            if(draftPack.PreviousOwner != null && timer.character == draftPack.PreviousOwner)
            {
                timer.IncreasePick();
            }
        }
    }

    private void OnDraftPackComplete(DraftPack completedPack)
    {
        if (CurrentRound.roundType != Round.RoundType.Draft)
        {
            Debug.LogWarning("Attempting to remove draft pack outside of draft.");
        }
        else
        {
            completedPack.DraftPackPassedEvent -= OnDraftPackPass;
            completedPack.DraftPackCompleteEvent -= OnDraftPackComplete;
            DraftPacks.Remove(completedPack);

            if (DraftPacks.Count == 0)
            {
                if (RoundIndex.Value == rounds.Count)
                {
                    Debug.LogError("Current last round is a draft. This shouldn't happen");
                    return;
                }
                EndPreviousRound();
            }
        }
    }

    private void PlayTickSound()
    {
        if (isTick)
        {
            tickSound.Play(false);
        }
        else
        {
            tockSound.Play(false);
        }

        isTick = !isTick;
    }

    private int GetArenaIndex(Character character)
    {
        if (character == null) return 0;
        var arenaGroup = GameManager.Instance.ArenaGroup;
        var isArenaGroupValid = arenaGroup > 0 && arenaGroup < CardsList.Count;
        return isArenaGroupValid ? arenaGroup : 0;
    }
    
    private DraftPack AssignDraftPack(Character character)
    {
        if (GameManager.Instance.DisableRP)
        {
            var cards = CardsList[0];
            var charms = CharmsList[0];
            return new DraftPack(character, cards, charms);    
        }
        else
        {
            var arenaIndex = GetArenaIndex(character);
            var cards = CardsList[arenaIndex];
            var charms = CharmsList[arenaIndex];
            return new DraftPack(character, cards, charms);    
        }
    }
    private void InitRandomDraftPackPools(int draftRound)
    {
        CardsList = new List<List<Card>>();
        CharmsList = new List<List<Charm>>();
        
        if (GameManager.Instance.DisableRP)
        {
            var draftPools = GlobalGameSettings.Settings.GetPackPoolForRound(draftRound);
            var pickedCharmDataList = PickCharmsData(draftPools.charmPool);
            var pickedCardDataList = PickCardsData(pickedCharmDataList.Count, draftPools.cardPool);
            PutCardsAndCharmsToPack(pickedCharmDataList, pickedCardDataList);
            return;
        }
        
        var draftPoolsList = GlobalGameSettings.Settings.GetPackPoolsForRound(draftRound);
        foreach (var draftPools in draftPoolsList)
        {
            var pickedCharmDataList = PickCharmsData(draftPools.charmPool);
            var pickedCardDataList = PickCardsData(pickedCharmDataList.Count, draftPools.cardPool);
            PutCardsAndCharmsToPack(pickedCharmDataList, pickedCardDataList);        
        }
    }
    private List<CharmData> PickCharmsData(List<CharmData> poolCharmsData)
    {
        List<CharmData> pickedCharms = new List<CharmData>();
        float random = UnityEngine.Random.Range(0f, 1f);
        if(random <= GlobalGameSettings.Settings.ChanceForDraftPacktoHaveCharm)
        {
            var charmIndexes = CreatePoolIndexList(poolCharmsData);
            int randomNumberOfCharms = UnityEngine.Random.Range(1, GlobalGameSettings.Settings.MaxNumberOfCharmsPerDraftPack);
            for (int i = 0; i < randomNumberOfCharms; i++)
            {
                if(charmIndexes.Count == 0)
                {
                    break;
                }

                int randomIndex = UnityEngine.Random.Range(0, charmIndexes.Count);
                pickedCharms.Add(poolCharmsData[randomIndex]);
                charmIndexes.RemoveAt(randomIndex);
            }
        }
        return pickedCharms;
    }
    private List<CardData> PickCardsData(int pickedCharmDataCount, List<CardData> poolCardsData)
    {
        List<CardData> pickedCardsData = new List<CardData>();
        var cardIndexes = CreatePoolIndexList(poolCardsData);

        for (int i = 0; i < GlobalGameSettings.Settings.DraftCardsPerPack; i++)
        {
            if(pickedCharmDataCount == 0 || i < GlobalGameSettings.Settings.DraftCardsPerPack - pickedCharmDataCount)
            {
                int randomIndex = UnityEngine.Random.Range(0, cardIndexes.Count);
                int cardIndex = cardIndexes[randomIndex];
                pickedCardsData.Add(poolCardsData[cardIndex]);
                cardIndexes.RemoveAt(randomIndex);
            }
        }
        return pickedCardsData;
    }
    private List<int> CreatePoolIndexList<T>(List<T> poolDatas)
    {
        List<int> indexes = new List<int>();
        for (int i = 0; i < poolDatas.Count; i++)
        {
            indexes.Add(i);
        }

        return indexes;
    }
    private void PutCardsAndCharmsToPack(List<CharmData> charmDataList, List<CardData> cardDataList)
    {
        var mergedList = new List<object>();
        mergedList.AddRange(charmDataList);
        mergedList.AddRange(cardDataList);
        mergedList = mergedList.OrderBy(item => Guid.NewGuid()).ToList();
        var cards = new List<Card>();
        var charms = new List<Charm>();
        
        foreach (var dataItem in mergedList)
        {
            if (dataItem is CharmData charmData)
            {
                charms.Add(new Charm(charmData));
            }
            if (dataItem is CardData cardData)
            {
                cards.Add(new Card(cardData));
            }
        }
        CardsList.Add(cards);
        CharmsList.Add(charms);
    }

#region Debug
    public void DebugToggleRoundTimerPause()
    {
        if(Debug.isDebugBuild)
        {
            DebugToggleRoundTimerPauseServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugToggleRoundTimerPauseServerRPC()
    {
        IsTimerPaused.Value = !IsTimerPaused.Value;
    }

    public void DebugSkipRound()
    {
        if (Debug.isDebugBuild)
        {
            DebugSkipRoundServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DebugSkipRoundServerRPC()
    {
        if (CurrentRound.roundType == Round.RoundType.Draft)
        {
            foreach (var character in characterManager.Characters)
            {
                character.ForceDraftPacksCompletion();
            }
        }
        else
        {
            CombatRoundTimer.Value = 0;
        }
    }
    #endregion
}
