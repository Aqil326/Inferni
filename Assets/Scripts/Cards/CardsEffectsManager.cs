using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Pool;

public struct CardAddedData
{
    public Card card;
    public Vector3 currentPosition;
    public Character targetCharacter;
}

public class PooledProjectileInstanceHandler : INetworkPrefabInstanceHandler
{
    CardsEffectsManager cardsEffectsManager;

    public PooledProjectileInstanceHandler(CardsEffectsManager pool)
    {
        cardsEffectsManager = pool;
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        return cardsEffectsManager.GetProjectile(position, rotation).GetComponent<NetworkObject>();
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        cardsEffectsManager.ReturnProjectile(networkObject.GetComponent<Projectile>());
    }
}

public class CardsEffectsManager : BaseManager
{
    [SerializeField]
    private Projectile projectilePrefab;

    [SerializeField]
    private CardProjectile cardProjectilePrefab;

    [SerializeField]
    private ProjectileView projectileViewPrefab;

    public List<Projectile> Projectiles { get; private set; } = new List<Projectile>();
    public List<CardProjectile> CardProjectiles { get; private set; } = new List<CardProjectile>();

    private ObjectPool<Projectile> projectilePool;
    private ObjectPool<ProjectileView> projectileViewPool;
    private CharacterManager characterManager;

    public float DebugProjectileSpeed = 1f;

    public override void Init()
    {
        characterManager = GameManager.GetManager<CharacterManager>();

        Projectile CreateFunc()
        {
            return Instantiate(projectilePrefab);
        }

        void ActionOnGet(Projectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        void ActionOnRelease(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        void ActionOnDestroy(Projectile projectile)
        {
            Destroy(projectile.gameObject);
        }

        projectilePool = new ObjectPool<Projectile>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(projectilePrefab.gameObject, new PooledProjectileInstanceHandler(this));
        }

        if (IsClient)
        {
            ProjectileView CreateViewFunc()
            {
                return Instantiate(projectileViewPrefab);
            }

            void ActionOnGetView(ProjectileView projectile)
            {
                projectile.gameObject.SetActive(true);
            }

            void ActionOnReleaseView(ProjectileView projectile)
            {
                projectile.gameObject.SetActive(false);
            }

            void ActionOnDestroyView(ProjectileView projectile)
            {
                Destroy(projectile.gameObject);
            }

            projectileViewPool = new ObjectPool<ProjectileView>(CreateViewFunc, ActionOnGetView, ActionOnReleaseView, ActionOnDestroyView);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(projectilePrefab.gameObject);
        }
    }

    private void OnEnable()
    {
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StartListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StartListening<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_HAND_WITH_POSITION_SERVER, CreateCardProjectileToHand);
        EventBus.StartListening<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_DECK_WITH_POSITION_SERVER, CreateCardProjectileToDeck);
        EventBus.StartListening(EventBusEnum.EventName.GAME_END_SERVER, OnGameEnd);
    }

    private void OnDisable()
    {
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StopListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StopListening<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_HAND_WITH_POSITION_SERVER, CreateCardProjectileToHand);
        EventBus.StopListening<CardAddedData>(EventBusEnum.EventName.CARD_ADDED_TO_DECK_WITH_POSITION_SERVER, CreateCardProjectileToDeck);
        EventBus.StopListening(EventBusEnum.EventName.GAME_END_SERVER, OnGameEnd);
    }

    private void StartCombatRound()
    {
        foreach(var projectile in Projectiles)
        {
            projectile.Unpause();
        }

        foreach(var c in CardProjectiles)
        {
            c.Unpause();
        }
    }

    private void EndCombatRound()
    { 
        foreach (var projectile in Projectiles)
        {
            projectile.Pause();
        }

        foreach (var c in CardProjectiles)
        {
            c.Pause();
        }
    }

    private void OnGameEnd()
    {
        var projectilesCopy = new List<Projectile>(Projectiles);
        foreach (var projectile in projectilesCopy)
        {
            projectile.DestroyProjectile();
        }
    }

    private void CreateCardProjectileToHand(CardAddedData cardData)
    {
        CreateCardProjectile(cardData, false);
    }

    private void CreateCardProjectileToDeck(CardAddedData cardData)
    {
        CreateCardProjectile(cardData, true);
    }

    private void CreateCardProjectile(CardAddedData data, bool toDeck)
    {
        var cardProjectile = Instantiate(cardProjectilePrefab);
        cardProjectile.GetComponent<NetworkObject>().Spawn();
        cardProjectile.MoveProjectile(data, toDeck, OnCardProjectileFinished);
        CardProjectiles.Add(cardProjectile);

        InitCardProjectileClientRPC(cardProjectile.NetworkObjectId, data.card.Data.InternalID);
    }

    [ClientRpc]
    private void InitCardProjectileClientRPC(ulong networkId, string cardId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var networkObject))
        {
            Debug.LogError($"Couldn't find card projectile with NetworkId of {networkId}");
            return;
        }
        var projectile = networkObject.GetComponent<CardProjectile>();
        projectile.SetCard(new Card(GameDatabase.GetCardData(cardId)));
    }

    private void OnCardProjectileFinished(CardProjectile cardProjectile)
    {
        cardProjectile.GetComponent<NetworkObject>().Despawn(true);
        CardProjectiles.Remove(cardProjectile);
    }

    public void ApplyProjectileEffects(Projectile projectile)
    {
        ApplyCardEffects(projectile.Owner, projectile.Card, projectile.Card.ProjectileCardEffects, projectile.Target);
    }

    public void ApplyCardEffects(Character sourceCharacter, Card card, List<CardEffect> effects, ITargetable target)
    {
        var targets = ChooseCardsTargets(sourceCharacter, target, card);

        foreach (var effect in effects)
        {
            effect.PreApplyEffect(targets);
        }

        foreach (var effect in effects)
        {
            effect.ApplyEffectToTargets(targets);
        }

        foreach (var effect in effects)
        {
            effect.PostApplyEffect(targets);
        }
    }

    public Projectile GetProjectile(Vector3 position, Quaternion rotation)
    {
        var projectile = projectilePool.Get();
        projectile.transform.position = position;
        projectile.transform.rotation = rotation;

        return projectile;
    }

    public void ReturnProjectile(Projectile projectile)
    {
        projectilePool.Release(projectile);
    }

    public void TryCreateProjectiles(Character sourceCharacter, Card card, ITargetable target)
    {
        if (card.ProjectileCardEffects.Count == 0)
        {
            return;
        }

        var targets = ChooseCardsTargets(sourceCharacter, target, card);

        foreach (var t in targets)
        {
            Debug.Log($"Target {t.TargetId}");
            //Apply effects immediately if projectile is aimed at Self
            if ((t is Character character && character == sourceCharacter) ||
                (t is CharmSlot slot && slot.Owner == sourceCharacter))
            {
                List<ITargetable> tempList = new List<ITargetable>();
                tempList.Add(t);

                foreach (var effect in card.ProjectileCardEffects)
                {
                    effect.PreApplyEffect(tempList);
                    effect.ApplyEffect(t, card);
                    effect.PostApplyEffect(tempList);
                }
            }
            else
            {
                InstantiateProjectile(card, sourceCharacter, target);
            }
        }
    }

    private void InstantiateProjectile(Card card, Character source, ITargetable target)
    {
        Projectile projectile = null;

        projectile = GetProjectile(source.ProjectileSpawnPosition, Quaternion.identity);
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.Init(card, target);
        
        if (!source.IsInCombat.Value)
        {
            projectile.Pause();
        }
        
        source.OnShotProjectile(projectile);
        projectile.Index.Value = source.ProjectileIndex;
        projectile.ProjectileDestroyedEvent += OnProjectileDisabled;
        Projectiles.Add(projectile);

        if(Debug.isDebugBuild)
        {
            projectile.AddSpeedMultiplier(DebugProjectileSpeed);
        }

        EventBus.TriggerEvent<Projectile>(EventBusEnum.EventName.PROJECTILE_SHOT_SERVER, projectile);
        ProjectileShotClientRPC(projectile.NetworkObjectId, ((ITargetable)projectile).TargetId);
    }

    public void DestroyProjectiles(Character character)
    {
        if (Projectiles.Count == 0) return;
        var projectilesCopy = new List<Projectile>(Projectiles);
        var toDestroyedProjectiles = projectilesCopy.Where(projectile => projectile.Owner == character);
        foreach (var projectile in toDestroyedProjectiles)
        {
            projectile.DestroyProjectile();
        }
    }

    [ClientRpc]
    private void ProjectileShotClientRPC(ulong projectileNetworkId, string projectileTargetId)
    {
        if(!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(projectileNetworkId, out var networkObject))
        {
            Debug.LogError($"Couldn't find projectile with NetworkId of {projectileNetworkId}");
            return;
        }
        var projectile = networkObject.GetComponent<Projectile>();
        Projectiles.Add(projectile);

        var view = projectileViewPool.Get();
        view.Init(projectile, projectileViewPool);

        LineManager.Instance.CreateLine(projectile.Owner, projectile.Target, projectileTargetId);
        EventBus.TriggerEvent<ProjectileView>(EventBusEnum.EventName.PROJECTILE_SHOT_CLIENT, view);
    }

    private void OnProjectileDisabled(Projectile projectile)
    {
        projectile.ProjectileDestroyedEvent -= OnProjectileDisabled;
        Projectiles.Remove(projectile);
        ProjectileDisabledClientRPC(projectile.NetworkObjectId);
        projectile.GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    private void ProjectileDisabledClientRPC(ulong projectileNetworkId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(projectileNetworkId, out var networkObject))
        {
            Debug.LogError($"Couldn't find projectile with NetworkId of {projectileNetworkId}");
            return;
        }
        var projectile = networkObject.GetComponent<Projectile>();
        Projectiles.Remove(projectile);
    }

    public List<ITargetable> ChooseCardsTargets(Character sourceCharacter, ITargetable target, Card card)
    {
        List<ITargetable> targets = new List<ITargetable>();
        switch(card.Data.Target)
        {
            case CardTarget.Any:
                targets.Add(target);
                break;
            case CardTarget.Spell:
                if(IsTargetSpell(target))
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.OpponentsSpell:
                if (IsTargetSpell(target) &&
                    (target is Projectile && ((Projectile) target).Owner != sourceCharacter))
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.Self:
                targets.Add(sourceCharacter);
                break;
            case CardTarget.AnyPlayer:
                if (IsTargetCharacter(target))
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.AnyOpponent:
                if(IsTargetCharacter(target) && ((Character) target).TeamIndex != sourceCharacter.TeamIndex)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.AllOpponents:
                targets.AddRange(characterManager.GetAllOpponents(sourceCharacter));
                break;
            case CardTarget.Teammate:
                if(IsTargetCharacter(target) && ((Character) target).TeamIndex != sourceCharacter.TeamIndex)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.All:
                var list = new List<Character>(characterManager.Characters);
                targets.AddRange(list);
                break;
            case CardTarget.EnergyPool:
                if(target is EnergyPool)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.World:
                if(target is World)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.CardInHand:
                if(target is Card)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.EmptyCharmSlot:
                if(target is CharmSlot charmSlot && charmSlot.IsEmpty)
                { 
                    targets.Add(target);
                }
                break;
            case CardTarget.Charm:
                if (target is CharmSlot charmSlot2 && !charmSlot2.IsEmpty)
                {
                    targets.Add(target);
                }
                break;
            case CardTarget.DownedPlayer:
                if (IsTargetCharacter(target) && ((Character) target).State.Value == CharacterState.Downed)
                {
                    targets.Add(target);    
                }
                break;
            case CardTarget.SpellInCast:
                if(target is CastingTargetable)
                {
                    targets.Add(target);
                }
                break;
        }

        return targets;
    }

    public bool IsProjectileDestroyed(Projectile projectile)
    {
        if (Projectiles == null || Projectiles.Count == 0) return true;
        return Projectiles.All(existedProjectile => existedProjectile != projectile);
    }

    private bool IsTargetSpell(ITargetable target)
    {
        return target != null && target is Projectile;
    }

    private bool IsTargetCharacter(ITargetable target)
    {
        return target != null && target is Character;
    }

    public void DebugSetProjectileSpeedMultiplier(float multiplier)
    {
        DebugProjectileSpeed = multiplier;

        DebugSetProjectileSpeedMultiplierServerRPC(multiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    private  void DebugSetProjectileSpeedMultiplierServerRPC(float multiplier)
    {
        DebugProjectileSpeed = multiplier;

        foreach(var p in Projectiles)
        {
            p.AddSpeedMultiplier(DebugProjectileSpeed);
        }
    }
}


