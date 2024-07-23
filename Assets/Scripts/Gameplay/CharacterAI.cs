using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class CharacterAI : MonoBehaviour
{
    private Character character;
    private List<float> draftPickTimes = new List<float>();
    [SerializeField]
    private float minCardPlayDelay = 2.5f;
    [SerializeField]
    private float maxCardPlayDelay = 6f;

    [SerializeField]
    private float minDraftPickDelay = 1.1f;
    [SerializeField]
    private float maxDraftPickDelay = 3.1f;
    private DraftPack nextPack = null;
    private float nextPickTime;

    private BotProfile profile;
    private Coroutine combatCoroutine;
    private Coroutine draftCoroutine;
    private CharacterManager characterManager;
    private CardsEffectsManager effectsManager;

    public void Init(BotProfile profile, Character character)
    {
        this.profile = profile;
        this.character = character;
        characterManager = GameManager.GetManager<CharacterManager>();
        effectsManager = GameManager.GetManager<CardsEffectsManager>();
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, OnStartCombat);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_SERVER, OnStartDraft);
    }

    private void OnStartCombat()
    {
        if(draftCoroutine != null)
        {
            StopCoroutine(draftCoroutine);
            draftCoroutine = null;
        }
        combatCoroutine = StartCoroutine(CombatCoroutine());
    }

    private void OnStartDraft()
    {
        if(combatCoroutine != null)
        {
            StopCoroutine(combatCoroutine);
            combatCoroutine = null;
        }
        draftCoroutine = StartCoroutine(DraftCoroutine());
    }

    private IEnumerator CombatCoroutine()
    {
        while(true)
        {
            while(!characterManager.AreBotsActive)
            {
                yield return null;
            }

            if(character.CanPlayCards())
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(minCardPlayDelay, maxCardPlayDelay));
                TryPlayCard();
            }

            yield return null;
        }
    }

    private IEnumerator DraftCoroutine()
    {
        while (true)
        {
            if (character.DraftPackBacklog.Count > 0)
            {
                DraftPack playerPack = character.DraftPackBacklog[0];
                yield return new WaitForSeconds(UnityEngine.Random.Range(minDraftPickDelay, maxDraftPickDelay));
                int highestScore = 0;
                string pickedId = "";

                foreach(var c in playerPack.Cards)
                {
                    int score = GetCardScore(c, null, false);
                    if(score >= highestScore)
                    {
                        highestScore = score;
                        pickedId = c.Data.InternalID;
                    }
                }

                foreach(var c in playerPack.Charms)
                {
                    int score = c.CharmData.score;
                    if(score >= highestScore)
                    {
                        highestScore = score;
                        pickedId = c.CharmData.InternalID;
                    }
                }
                character.PickFromDraftPack(pickedId, playerPack, out Card card, out Charm charm); 
            }

            yield return null;
        }
    }

    private void TryPlayCard()
    {
        if(character.HandSize == 0)
        {
            return;
        }

        Card selectedCard = null;
        ITargetable selectedTarget = null;
        int highestCardScore = 0;

        for(int i = 0; i < character.Hand.Length; i++)
        {
            Card card = character.Hand[i];
            if (card != null)
            {
                switch (card.Data.Target)
                {
                    case CardTarget.AnyPlayer:
                        foreach (var c in characterManager.Characters)
                        {
                            TrySelectCard(card, c, ref highestCardScore, ref selectedCard, ref selectedTarget);
                        }
                        break;
                    case CardTarget.AnyOpponent:
                        foreach (var c in characterManager.Characters)
                        {
                            if (c.TeamIndex != character.TeamIndex)
                            {
                                TrySelectCard(card, c, ref highestCardScore, ref selectedCard, ref selectedTarget);
                            }
                        }
                        break;
                    case CardTarget.CardInHand:
                        foreach (var c in character.Hand)
                        {
                            if (c != null)
                            {
                                TrySelectCard(card, c, ref highestCardScore, ref selectedCard, ref selectedTarget);
                            }
                        }
                        break;
                    case CardTarget.Charm:
                        foreach (var c in characterManager.Characters)
                        {
                            foreach (var slot in c.CharmSlots)
                            {
                                if (!slot.IsEmpty)
                                {
                                    TrySelectCard(card, slot, ref highestCardScore, ref selectedCard, ref selectedTarget);
                                }
                            }
                        }
                        break;
                    case CardTarget.DownedPlayer:
                        foreach (var c in characterManager.Characters)
                        {
                            if(c.State.Value == CharacterState.Downed)
                            {
                                TrySelectCard(card, c, ref highestCardScore, ref selectedCard, ref selectedTarget);
                            }
                        }
                        break;
                    case CardTarget.EmptyCharmSlot:
                        foreach (var c in characterManager.Characters)
                        {
                            foreach (var slot in c.CharmSlots)
                            {
                                if (slot.IsEmpty)
                                {
                                    TrySelectCard(card, slot, ref highestCardScore, ref selectedCard, ref selectedTarget);
                                }
                            }
                        }
                        break;
                    case CardTarget.OpponentsSpell:
                        foreach(var p in effectsManager.Projectiles)
                        {
                            if(p.Owner.TeamIndex != character.TeamIndex)
                            {
                                TrySelectCard(card, p, ref highestCardScore, ref selectedCard, ref selectedTarget);
                            }
                        }
                        break;
                    case CardTarget.Self:
                        TrySelectCard(card, character, ref highestCardScore, ref selectedCard, ref selectedTarget);
                        break;
                    case CardTarget.Spell:
                        foreach (var p in effectsManager.Projectiles)
                        {
                            TrySelectCard(card, p, ref highestCardScore, ref selectedCard, ref selectedTarget);
                        }
                        break;
                    case CardTarget.SpellInCast:
                        foreach (var c in characterManager.Characters)
                        {
                            TrySelectCard(card, c.CastingTargetable, ref highestCardScore, ref selectedCard, ref selectedTarget);
                        }
                        break;
                    case CardTarget.Teammate:
                        foreach (var c in characterManager.Characters)
                        {
                            if(c.TeamIndex == character.TeamIndex && c != character)
                            {
                                TrySelectCard(card, c, ref highestCardScore, ref selectedCard, ref selectedTarget);
                            }
                        }
                        break;
                    case CardTarget.Any:
                    case CardTarget.World:
                        TrySelectCard(card, GameManager.Instance.World, ref highestCardScore, ref selectedCard, ref selectedTarget);
                        break;
                }
            }
        }

        if(selectedCard != null)
        {
            Debug.Log($"Bot {character.PlayerData.DisplayName} playing card {selectedCard.Data.Name} on target {selectedTarget.TargetId}");
            character.TryPlayCardFromServer(selectedCard.HandIndex, selectedTarget);
        }
    }

    private void TrySelectCard(Card card,
                                ITargetable target,
                                ref int highestCardScore,
                                ref Card selectedCard,
                                ref ITargetable selectedTarget)
    {
        int cardScore = GetCardScore(card, target);
        Debug.Log($"Bot {character.PlayerData.DisplayName} checking {card.Data.Name} on target {target.TargetId}. Score : {cardScore}");
        if (cardScore > highestCardScore)
        {
            highestCardScore = cardScore;
            selectedCard = card;
            selectedTarget = target;
        }
        else if(cardScore == highestCardScore)
        {
            //If a cardScore ties with the highscore it's a 50/50 chance to switch target
            if(Random.Range(0f,1f) > 0.5f)
            {
                selectedCard = card;
                selectedTarget = target;
            }
        }
    }

    private int GetCardScore(Card card, ITargetable targetable, bool checkCastability = true)
    {
        if (checkCastability && (!card.IsCastable()  || card.Data.IsBlocked || !card.IsTargetableValid(targetable)))
        {
            return GlobalGameSettings.Settings.BotSettings.UnplayableCardScore;
        }

        int totalCardScore = card.Data.BaseScore;

        int lifeRemainingBonus = 0;

        var categoryScoreDictionary = profile.CardCategoryScores;

        var cardEffects = card.GetAllCardEffects();
        var cardTargets = new List<ITargetable>();
        foreach (var e in cardEffects)
        {
            if (targetable == null)
            {
                foreach (var c in e.Data.CardEffectCategories)
                {
                    if (categoryScoreDictionary.TryGetValue(c, out int value))
                    {
                        totalCardScore += GetProfileCardScore(value, null, GlobalGameSettings.Settings.BotSettings.GetCardCategoryPositiveness(c));
                    }
                }
                continue;
            }

            cardTargets.Clear();
            cardTargets.Add(targetable);
            var targets = e.GetEffectTargets(cardTargets);
            foreach(var t in targets)
            {
                Character characterTarget = null;
                if (t is Character)
                {
                    characterTarget = t as Character;

                    //Don't try to play card to Dead characters
                    //TODO: Change this if we ever have Dead characters as a possible target
                    if(characterTarget.State.Value == CharacterState.Dead)
                    {
                        return GlobalGameSettings.Settings.BotSettings.UnplayableCardScore;
                    }
                }

              
                bool isTargetSelf = (characterTarget != null && characterTarget == character);
                bool isTargetTeammate = (characterTarget != null && characterTarget.TeamIndex == character.TeamIndex && !isTargetSelf);

                foreach (var c in e.Data.CardEffectCategories)
                {
                    if (categoryScoreDictionary.TryGetValue(c, out int value))
                    {
                        totalCardScore += GetProfileCardScore(value, characterTarget, GlobalGameSettings.Settings.BotSettings.GetCardCategoryPositiveness(c));
                    }

                    if (c == CardEffectCategory.Buff)
                    {
                        lifeRemainingBonus += GetLifeRemainingHealBonus(characterTarget, (isTargetSelf || isTargetTeammate));
                    }
                    else if (c == CardEffectCategory.Damage)
                    {
                        //If a bot sees a kill, prioritize this target
                        int damage = card.GetCardEffectScore(t);
                        if(damage >= characterTarget.Health.Value)
                        {
                            totalCardScore += GlobalGameSettings.Settings.BotSettings.KillBonus * ((isTargetTeammate || isTargetSelf)? -1 : 1);
                        }

                        if(characterTarget != null && characterTarget.State.Value == CharacterState.Downed)
                        {
                            totalCardScore += GlobalGameSettings.Settings.BotSettings.DownedBonus * ((isTargetTeammate || isTargetSelf) ? -1 : 1);
                        }


                        totalCardScore += profile.AgressivenessBonus;
                    }
                    else if (c == CardEffectCategory.Defense)
                    {
                        lifeRemainingBonus += GetLifeRemainingHealBonus(characterTarget, (isTargetSelf || isTargetTeammate));
                    }
                    if (c == CardEffectCategory.Healing)
                    {
                        lifeRemainingBonus += GetLifeRemainingHealBonus(characterTarget, (isTargetSelf || isTargetTeammate));

                        if (characterTarget != null && characterTarget.State.Value == CharacterState.Downed)
                        {
                            totalCardScore += GlobalGameSettings.Settings.BotSettings.DownedBonus * (isTargetTeammate ? 1 : -1);
                        }
                    }
                    if (c == CardEffectCategory.Revive)
                    {
                        totalCardScore += GlobalGameSettings.Settings.BotSettings.ReviveScoreBonus * ((isTargetTeammate) ? 1 : -1);
                    }
                    if (c == CardEffectCategory.SelfDamage)
                    {
                        lifeRemainingBonus += GetLifeRemainingDamageBonus(character, true);
                    }
                    if (c == CardEffectCategory.SelfHeal)
                    {
                        lifeRemainingBonus += GetLifeRemainingHealBonus(character, true);
                    }
                }

                //The effect score is positive if the card effect is positive and negative if the card effect is negative
                int effectScore = card.GetCardEffectScore(t);
                if (effectScore > 0)
                {
                    if (isTargetTeammate)
                    {
                        totalCardScore -= profile.SelfishnessBonus;
                    }
                    else if (isTargetSelf)
                    {
                        totalCardScore += profile.SelfishnessBonus;
                    }
                }
                if(characterTarget != null)
                {
                    effectScore *= ((isTargetSelf || isTargetTeammate) ? 1 : -1);
                }
                totalCardScore += effectScore;
            }
        }
        
        totalCardScore += lifeRemainingBonus;
        return totalCardScore;
    }

    private int GetLifeRemainingHealBonus(Character characterTarget, bool isTargetSameTeam)
    {
        if(characterTarget == null)
        {
            return 0;
        }
        if(characterTarget.Data.MaxHealth == characterTarget.Health.Value && isTargetSameTeam)
        {
            //if Health is full
            return GlobalGameSettings.Settings.BotSettings.FullHealthHealDeduction;
        }

        return ((characterTarget.Data.MaxHealth - characterTarget.Health.Value) *
                            GlobalGameSettings.Settings.BotSettings.LifeRemainingHealBonus *
                            ((isTargetSameTeam) ? 1 : -1));
    }

    private int GetLifeRemainingDamageBonus(Character characterTarget, bool isTargetSameTeam)
    {
        if (characterTarget == null)
        {
            return 0;
        }
        return (characterTarget.Data.MaxHealth - characterTarget.Health.Value) *
                            GlobalGameSettings.Settings.BotSettings.LifeRemainingDamageBonus *
                            ((isTargetSameTeam) ? -1 : 1);
    }

    private int GetProfileCardScore(int value, Character target, bool isPositive)
    {
        if(target == null)
        {
            return value;
        }
        bool isTargetSelf = target == character;
        bool isTargetTeammate = target.TeamIndex == character.TeamIndex && !isTargetSelf;
        int multiplier = isPositive ? 1 : -1; 
        return (isTargetTeammate || isTargetSelf) ? value * multiplier : value * -multiplier;
    }

    private void OnDestroy()
    {
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, OnStartCombat);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_SERVER, OnStartDraft);
    }
}


