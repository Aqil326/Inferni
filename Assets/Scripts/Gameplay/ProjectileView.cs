using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

public class ProjectileView : TargetableView, IPointerEnterHandler, IPointerExitHandler
{
    private const float SCALE_DEFAULT = 0.5f;
    private const float SCALE_SMALL = 0.3f;

    [SerializeField]
    private IconWithMaskController icon;

    [SerializeField]
    private Image selectedHighlight;
    [SerializeField]
    private SoundManager.Sound selectedSound;

    [SerializeField]
    private SpriteRenderer energyOrb;

    [SerializeField]
    private float orbSpinSpeed = 0.5f;

    [SerializeField]
    private Image projectileNumberParent;

    [SerializeField]
    private TextMeshProUGUI projectileNumber;

    [SerializeField]
    private GameObject prefabLine;

    [SerializeField]
    private Collider collider;

    [SerializeField]
    private Renderer renderer;

    [SerializeField]
    private AudioSource audioSourceSingle;
    [SerializeField]
    private AudioSource audioSourceLoop;

    [SerializeField]
    private Particle damageParticle;
    [SerializeField]
    private Particle healParticle;
    [SerializeField]
    private Particle otherParticle;

    private Particle activeParticle;
    
    [SerializeField]
    private Renderer damageOuterRenderer;
    [SerializeField]
    private Renderer healOuterRenderer;
    [SerializeField]
    private Renderer otherOuterRenderer;

    [SerializeField]
    private Material outlineMaterial;
    [SerializeField]
    private Color selectedOutlineColor;

    public Projectile Projectile { get; private set; }

    private bool isPointerOver;
    private bool enablePointerEvents;
    private ObjectPool<ProjectileView> pool;
    private CharacterManager characterManager;
    private Material outlineMaterialInstance;
    private Color initialOutlineColor;
    private int initialLayer;

    private void Awake()
    {
        characterManager = GameManager.GetManager<CharacterManager>();
        outlineMaterialInstance = new Material(outlineMaterial);
        initialOutlineColor = outlineMaterialInstance.GetColor("_OutlineColor");
        initialLayer = gameObject.layer;
    }

    public void Init(Projectile projectile, ObjectPool<ProjectileView> pool)
    {
        if (this.Projectile != null)
        {
            ClearProjectile();
        }
        this.Projectile = projectile;
        this.pool = pool;
        projectile.ProjectileDestroyedEvent += OnProjectileDestroyed;
        projectile.ProjectileTargetChangedEvent += OnProjectileChangedTarget;
        enablePointerEvents = true;
        SetTargetable(projectile);
        energyOrb.color = GlobalGameSettings.Settings.GetCardColor(projectile.Card.Data.CardColor);
        selectedHighlight.enabled = false;
        icon.InitIcon(projectile.Card.Data.CardSprite);
        projectileNumberParent.gameObject.SetActive(false);
        SetProjectileScale();
        transform.position = projectile.transform.position;
        ClearParticles();
        SetParticles(projectile.type);
        SetOuterRenderer(projectile.type);

        Projectile.Card.Data.EndCastSound.Play(false, audioSourceSingle);
        Projectile.Card.Data.ProjectileTravelSound.Play(true, audioSourceLoop);
    }

    private void ClearParticles()
    {
        if (activeParticle != null)
        {
            activeParticle.Clear();
        }
    }

    public void ClearProjectile()
    {
        Projectile.ProjectileTargetChangedEvent -= OnProjectileChangedTarget;
        Projectile.ProjectileDestroyedEvent -= OnProjectileDestroyed;
        Projectile = null;
    }

    private void OnProjectileChangedTarget(Projectile projectile, string oldProjectileId)
    {
        SetProjectileScale();
        if (projectile.Target is not Character && projectile.Target is not CharmSlot) return;
        
        var newProjectileId = ((ITargetable)projectile).TargetId;
        if (string.IsNullOrEmpty(oldProjectileId) || newProjectileId == oldProjectileId) return;
        
        LineManager.Instance.SwitchLineDirection(oldProjectileId, newProjectileId, projectile.Owner, projectile.Target);

        EventBus.TriggerEvent<ProjectileView>(EventBusEnum.EventName.PROJECTILE_TARGET_CHANGED_CLIENT, this);
    }

    private void OnProjectileDestroyed(Projectile projectile)
    {
        ClearHoverEffect(true);
        BlockPointerEvents();
        ClearProjectile();
        AnimateDestroy();
    }

    private void SetProjectileScale()
    {
        ITargetable playerTarget = characterManager.PlayerCharacter;
        gameObject.transform.localScale =
           Projectile.Target == playerTarget ? Vector3.one * SCALE_DEFAULT : Vector3.one * SCALE_SMALL;
    }


    public void ShowProjectileNumber()
    {
        if (Projectile.HasProjectileNumber)
        {
            var colorForEffect = GlobalGameSettings.Settings.GetEffectColors(Projectile.EffectType);
            projectileNumberParent.color = colorForEffect.BackgroundColor;
            projectileNumber.color = colorForEffect.TextColor;
            projectileNumber.text = Projectile.ProjectileNumber.ToString();
        }

        projectileNumberParent.gameObject.SetActive(Projectile.HasProjectileNumber);
    }

    public override void TargetSelected(bool isValidTarget)
    {
        if (!isValidTarget) return;
        selectedHighlight.enabled = true;
        selectedSound.Play(false);
        outlineMaterialInstance.SetColor("_OutlineColor", selectedOutlineColor);
    }

    public override void TargetDeselected()
    {
        selectedHighlight.enabled = false;
        outlineMaterialInstance.SetColor("_OutlineColor", initialOutlineColor);
    }

    private void Update()
    {
        if(Projectile != null)
        {
            transform.position = Projectile.transform.position;
            ShowProjectileNumber();
        }
        energyOrb.gameObject.transform.Rotate(new Vector3(0f, 0f, orbSpinSpeed));
    }

    private void AnimateDestroy()
    {
        StartCoroutine(DestroyAnimation());
    }

    private IEnumerator DestroyAnimation()
    {

        var targetColor = new Color(0, 0, 0, 0);
        var oldPosition = transform.position;
        var targetPosition = transform.position + (transform.up * 1f);
        var steps = 50;
        float stepIndex;
        float smoothstep;
        activeParticle.Stop();
        for (int i = 1; i <= steps; i++)
        {
            stepIndex = (float)i / steps;
            smoothstep = stepIndex * stepIndex * (3f - (2f * stepIndex));
            icon.SetAlpha(1f - smoothstep);
            transform.position = Vector3.Lerp(oldPosition, targetPosition, smoothstep);
            yield return new WaitForEndOfFrame();
        }
        pool.Release(this);
        icon.SetAlpha(1f);
    }
    
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if(!enablePointerEvents)
        {
            return;
        }

        LineManager.Instance.ShowLine(((ITargetable)Projectile).TargetId, Projectile.Owner, Projectile.Target);

        isPointerOver = true;
        var cardInspectedData = new CardInspectedData()
        {
            card = Projectile.Card,
            InFlight = true,
            inspectedRect = null,
            tooltipPivot = new Vector3(0.5f, 1)
        };
        EventBus.TriggerEvent<CardInspectedData>(EventBusEnum.EventName.CARD_INSPECTED_CLIENT, cardInspectedData);
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (!enablePointerEvents)
        {
            return;
        }

        ClearHoverEffect(false);
    }

    public void ClearHoverEffect(bool isDestroyed)
    {
        if (isDestroyed)
        {
            RemoveLinesAfterDestroyed(((ITargetable)Projectile).TargetId);
        }
        else
        {
            LineManager.Instance.HideLines(Projectile.Owner, Projectile.Target);
            var targetIsProjectile = Projectile.Target.TargetId.IndexOf("#Projectile", StringComparison.Ordinal) > -1;
            if (targetIsProjectile)
            {
                var targetProjectile = (Projectile)Projectile.Target;
                if (targetProjectile != null)
                {
                    LineManager.Instance.HideLines(targetProjectile.Owner, targetProjectile.Target);    
                }
            }
        }
        
        if (isPointerOver)
        {
            ClearPreviewCard();    
        }

        isPointerOver = false;
    }

    private void RemoveLinesAfterDestroyed(string projectileId)
    {
        LineManager.Instance.RemoveLine(projectileId);
       
        var targetIsProjectile = Projectile.Target.TargetId.IndexOf("#Projectile", StringComparison.Ordinal) > -1;
        if (!targetIsProjectile) return;
        
        var targetProjectile = (Projectile)Projectile.Target;
        if (targetProjectile == null) return;
        
        LineManager.Instance.RemoveLine(((ITargetable)targetProjectile).TargetId);
    }

    private void ClearPreviewCard()
    {
        EventBus.TriggerEvent<Card>(EventBusEnum.EventName.CLEAR_CARD_INSPECTED_CLIENT, Projectile.Card);
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    private void SetParticles(Projectile.ProjectileType type)
    {
        damageParticle.gameObject.SetActive(false);
        healParticle.gameObject.SetActive(false);
        otherParticle.gameObject.SetActive(false);

        switch (type)
        {
            case Projectile.ProjectileType.Damage:
                damageParticle.gameObject.SetActive(true);
                activeParticle = damageParticle;
                break;
            case Projectile.ProjectileType.Heal:
                healParticle.gameObject.SetActive(true);
                activeParticle = healParticle;
                break;
            case Projectile.ProjectileType.Other:
                otherParticle.gameObject.SetActive(true);
                activeParticle = otherParticle;
                break;
        }
    }
    
    private void SetOuterRenderer(Projectile.ProjectileType type)
    {
        switch (type)
        {
            case Projectile.ProjectileType.Damage:
                damageOuterRenderer.gameObject.SetActive(true);
                healOuterRenderer.gameObject.SetActive(false);
                otherOuterRenderer.gameObject.SetActive(false);
                break;
            case Projectile.ProjectileType.Heal:
                damageOuterRenderer.gameObject.SetActive(false);
                healOuterRenderer.gameObject.SetActive(true);
                otherOuterRenderer.gameObject.SetActive(false);
                break;
            case Projectile.ProjectileType.Other:
                damageOuterRenderer.gameObject.SetActive(false);
                healOuterRenderer.gameObject.SetActive(false);
                otherOuterRenderer.gameObject.SetActive(true);
                break;
        }
    }

    public void BlockPointerEvents()
    {
        if(isPointerOver)
        {
            ClearHoverEffect(false);
        }

        enablePointerEvents = false;
    }

    protected override void OnCardDragStarted(Card card)
    {
        bool isTarget = card.Data.Target == CardTarget.Spell ||
            card.Data.Target == CardTarget.SpellInCast ||
            card.Data.Target == CardTarget.Any;
        collider.enabled = isTarget;
        if(isTarget)
        {
            List<Material> materials = new List<Material>(renderer.sharedMaterials);
            materials.Add(outlineMaterialInstance);
            renderer.SetMaterials(materials);

            renderer.gameObject.SetGameLayerRecursive(LayerMask.NameToLayer("Outline"));
        }
    }

    protected override void OnCardDragEnded(Card card)
    {
        if (renderer.gameObject.layer != initialLayer)
        {
            List<Material> materials = new List<Material>(renderer.sharedMaterials);
            materials.Remove(outlineMaterialInstance);
            renderer.SetMaterials(materials);

            renderer.gameObject.SetGameLayerRecursive(initialLayer);
        }
        collider.enabled = true;
    }
}
