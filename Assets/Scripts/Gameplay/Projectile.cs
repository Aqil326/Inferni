using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour, ITargetable
{
    [SerializeField]
    private float minDistanceCollisionCheck = 0.1f;

    public Card Card { get; private set; }
    public Character Owner { get; private set; }
    public ITargetable Target { get; private set; }
    public int ProjectileNumber { get; private set; }
    public bool HasProjectileNumber { get; private set; }
    public CardEffectCategory EffectType { get; private set; }

    public NetworkVariable<int> OwnerHealthWhenCast = new NetworkVariable<int>();
    public NetworkVariable<float> LifeTime = new NetworkVariable<float>();
    public NetworkVariable<int> Index = new NetworkVariable<int>();
    public NetworkVariable<float> LastXCalculationTime = new NetworkVariable<float>();
    public NetworkVariable<float> TimeToContact = new NetworkVariable<float>();
    public NetworkVariable<float> TimeInHand = new NetworkVariable<float>();


    public static readonly string TargetIdSymbol = "#Projectile";
    Vector3 ITargetable.Position => transform.position;
    //TODO: Add projectile number
    string ITargetable.TargetId => Owner.PlayerData.Id + TargetIdSymbol + Index.Value;

    public event Action<Projectile> ProjectileDestroyedEvent;
    public event Action<Projectile, string> ProjectileTargetChangedEvent;

    private CharacterManager characterManager;
    private CardsEffectsManager effectsManager;
    private float speed;
    private Coroutine projectileMoveCoroutine;
    private bool isPaused;

    public enum ProjectileType { Damage, Heal, Other };
    public ProjectileType type;

    private void Awake()
    {
        effectsManager = GameManager.GetManager<CardsEffectsManager>();
        characterManager = GameManager.GetManager<CharacterManager>();
    }

    public void Init(Card card, ITargetable projectileTarget)
    {
        Card = card;
        Card.XData = new XData();
        Card.SetProjectile(this);
        Target = projectileTarget;
        Owner = Card.Owner;
        Index.Value = card.Owner.ProjectileIndex;
        speed = card.ProjectileSpeed;
        OwnerHealthWhenCast.Value = Owner.Health.Value;
        TimeInHand.Value = Card.TimeInHand;
        LifeTime.Value = 0;
        LastXCalculationTime.Value = 0;
        type = Card.Data.ProjectileType;

        foreach(var effect in Card.ProjectileCardEffects)
        {
            effect.SetProjectile(this);
        }

        MoveProjectile();
       
        InitProjectileClientRPC(Card.Data.InternalID, Owner.PlayerData.Id, Target.TargetId);
    }

    [ClientRpc]
    private void InitProjectileClientRPC(string cardId, ulong ownerId, string targetId)
    {
        if (!IsHost)
        {
            Owner = characterManager.Characters.Find(c => c.PlayerData.Id == ownerId);
            Card = new Card(GameDatabase.GetCardData(cardId));
            Card.SetOwner(Owner);
            Card.XData = new XData();
            Target = characterManager.GetTargetable(targetId);
            Card.SetProjectile(this);
        }
    }


    public void MoveProjectile()
    {
        if (projectileMoveCoroutine != null)
        {
            StopCoroutine(projectileMoveCoroutine);
        }
        projectileMoveCoroutine = StartCoroutine(ProjectileMoveCoroutine());
    }

    public void AddSpeedMultiplier(float multiplier)
    {
        speed = speed * multiplier;
    }

    public void DestroyProjectile()
    {
        if (projectileMoveCoroutine != null)
        {
            StopCoroutine(projectileMoveCoroutine);
        }

        ProjectileDestroyedClientRPC();
        ProjectileDestroyedEvent?.Invoke(this);
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }

    private IEnumerator ProjectileMoveCoroutine()
    {
        float distance = Vector3.Distance(Target.Position, transform.position);
        TimeToContact.Value = distance / speed;
        while (distance > minDistanceCollisionCheck)
        {
            yield return null;
            if (!isPaused)
            {
                if (Target is Projectile projectile)
                {
                    if (effectsManager.IsProjectileDestroyed(projectile))
                    {
                        //Target has been destroyed, destroy projectile
                        DestroyProjectile();
                        yield break;
                    }
                }

                distance = Vector3.Distance(Target.Position, transform.position);
                TimeToContact.Value = distance / speed;
                transform.position += (Target.Position - transform.position).normalized * Time.deltaTime * speed;
                LifeTime.Value += Time.deltaTime;

                var target = Target is Character ? (Character)Target : null;
                if (Card.TryUpdateCardXData(CardXPreviewType.Flying, target))
                {
                    UpdateCardXDataClientRPC(Card.XData);
                }

                UpdateProjectileNumber();
            }
        }

        projectileMoveCoroutine = null;

        var shouldApplyEffect = true;
        var shouldMirrorProjectile = Target is Character || Target is CharmSlot;
        if (shouldMirrorProjectile)
        {
            var targetCharacter = Target is Character character ? character : ((CharmSlot)Target).Owner;
            if (targetCharacter.Modifier is OnProjectileHitModifier modifier)
            {
                shouldApplyEffect = modifier.OnProjectileHit(this, targetCharacter);    
            }
        }
        if (shouldApplyEffect)
        {
            ApplyEffects();
        }
    }

    [ClientRpc]
    private void UpdateCardXDataClientRPC(XData xData)
    {
        Card.XData = xData;
    }

    private void UpdateProjectileNumber()
    {
        if (Card is not { ProjectileCardEffects: { Count: > 0 } }) return;

        int number = 0;
        CardEffectCategory effectType = CardEffectCategory.Other;
        HasProjectileNumber = true;
        ProjectileNumber = 0;

        foreach (var eff in Card.ProjectileCardEffects)
        {
            if (eff.IsProjectileNumberVisible(ref number, ref effectType))
            {
                if (EffectType == effectType)
                {
                    ProjectileNumber += number;
                }
                else
                {
                    ProjectileNumber = number;
                }
                EffectType = effectType;
            }
            else
            {
                HasProjectileNumber = false;
            }
        }

        UpdateProjectileNumberClientRPC(HasProjectileNumber, ProjectileNumber, EffectType);
        
    }

    [ClientRpc]
    private void UpdateProjectileNumberClientRPC(bool hasProjectileNumber, int projectileNumber, CardEffectCategory effectType)
    {
        HasProjectileNumber = hasProjectileNumber;
        ProjectileNumber = projectileNumber;
        EffectType = effectType;
    }

    private void ApplyEffects()
    {
        effectsManager.ApplyProjectileEffects(this);
        Card.Data.ProjectileHitSound.Play(false, transform.position);

        DestroyProjectile();
    }

    public void ChangeTarget(Character newOwner, ITargetable target)
    {
        var oldProjectileId = ((ITargetable)this).TargetId;
        Owner = newOwner;
        Target = target;
     
        ProjectileTargetChangedEvent?.Invoke(this, oldProjectileId);
        ProjectileTargetChangedClientRPC(newOwner.PlayerData.Id, target.TargetId, oldProjectileId);
    }

    [ClientRpc]
    private void ProjectileTargetChangedClientRPC(ulong ownerId, string targetId, string oldProjectileId)
    {
        if(IsHost)
        {
            return;
        }

        Owner = characterManager.Characters.Find(c => c.PlayerData.Id == ownerId);
        Target = characterManager.GetTargetable(targetId);
        ProjectileTargetChangedEvent?.Invoke(this, oldProjectileId);
    }

    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        return false;
    }

    [ClientRpc]
    private void ProjectileDestroyedClientRPC()
    {
        if(IsHost)
        {
            return;
        }
        ProjectileDestroyedEvent?.Invoke(this);
    }
}
