using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldView : TargetableView, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject selectedHighlight;
    [SerializeField]
    private SpriteRenderer[] selectedHighlightImages;
    [SerializeField]
    public Color unselectedColor;
    [SerializeField]
    public Color selectedColor;
    [SerializeField]
    private SoundManager.Sound selectedSound;
    [SerializeField]
    private Transform modifierEffectParent;
    [SerializeField]
    private Transform modifierUIParent;
    [SerializeField]
    private ModifierUI modifierUIPrefab;
    [SerializeField]
    private Collider collider;
    [SerializeField]
    private Renderer[] outlineRenderers;
    [SerializeField]
    private GameObject[] outlineLayerObjects;
    [SerializeField]
    private Material outlineMaterial;
    [SerializeField]
    private Color selectedOutlineColor;
    [SerializeField]
    private ParticleSystem deathTickParticle;
    [SerializeField]
    private int timeToPlayDeathTick = 30;
    [SerializeField]
    private float deathTickDonutRadius = 12;
    [SerializeField]
    private float maxParticleDonutRadius = 12;
    [SerializeField]
    private float postDeathTickDonutShrinkTimer = 15;
    [SerializeField]
    private GameObject modelsFor4Teams;
    [SerializeField]
    private GameObject modelsFor3Teams;
    [SerializeField]
    private GameObject worldModelsParent;
    [SerializeField]
    private GameObject trainingArenaModelsParent;

    private ModifierUI modifierUI;
    private GameObject modifierEffect;
    private World world;
    private Material outlineMaterialInstance;
    private Color initialOutlineColor;
    private int initialLayer;
    private RoundManager roundManager;

    protected override void Start()
    {
        worldModelsParent.SetActive(false);
        trainingArenaModelsParent.SetActive(false);
        base.Start();
        selectedHighlight.SetActive(false);


        modifierUI = Instantiate(modifierUIPrefab, modifierUIParent);
        modifierUI.gameObject.SetActive(false);

        outlineMaterialInstance = new Material(outlineMaterial);
        initialOutlineColor = outlineMaterialInstance.GetColor("_OutlineColor");
        initialLayer = gameObject.layer;
    }

    public void Init(World world)
    {
        this.world = world;
        worldModelsParent.SetActive(true);
        trainingArenaModelsParent.SetActive(true);
        SetTargetable(world);

        roundManager = GameManager.GetManager<RoundManager>();
        roundManager.CombatRoundTimer.OnValueChanged += OnCombatTimerChanged;

        var characterManager = GameManager.GetManager<CharacterManager>();
        int teamsCount = characterManager.NumberOfTeamsAlive;

        trainingArenaModelsParent.SetActive(GameManager.Instance.IsTrainingMode);
        worldModelsParent.SetActive(!GameManager.Instance.IsTrainingMode);

        modelsFor3Teams.SetActive(teamsCount % 3 == 0);
        modelsFor4Teams.SetActive(teamsCount % 3 != 0);

        Camera.main.farClipPlane = GlobalGameSettings.Settings.GameplayCameraFarPlane;
    }

    public void AddModifier(GlobalModifier modifier, NetworkVariable<float> remainingDuration)
    {
        modifier.ApplyVisualEffects(this);

        modifierUI.gameObject.SetActive(true);
        modifierUI.AddModifier(modifier.Data, modifier.Duration, remainingDuration);
    }

    public void RemoveModifier(GlobalModifier modifier)
    {
        modifier.RemoveVisualEffects(this);
        modifierUI.gameObject.SetActive(false);
        modifierUI.Clear();
    }

    public void AddModifierEffect(GameObject prefab)
    {
        modifierEffect = Instantiate(prefab, modifierEffectParent);
        modifierEffect.transform.localPosition = Vector3.zero;
    }

    public void RemoveModifierEffect()
    {
        if (modifierEffect != null)
        {
            Destroy(modifierEffect);
        }
    }

    public override void TargetSelected(bool isValidTarget)
    {
        selectedHighlight.SetActive(true);
        foreach (var sprite in selectedHighlightImages)
        {
            sprite.color = isValidTarget ? selectedColor : unselectedColor;
        }
        if (isValidTarget) selectedSound.Play(false);
        outlineMaterialInstance.SetColor("_OutlineColor", selectedOutlineColor);
    }

    public override void TargetDeselected()
    {
        selectedHighlight.SetActive(false);
        outlineMaterialInstance.SetColor("_OutlineColor", initialOutlineColor);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    protected override void OnCardDragStarted(Card card)
    {
        bool isTarget = card.Data.Target == CardTarget.World ||
            card.Data.Target == CardTarget.Any;
        collider.enabled = isTarget;
        if (isTarget)
        {
            foreach (var r in outlineRenderers)
            {
                List<Material> materials = new List<Material>(r.sharedMaterials);
                materials.Add(outlineMaterialInstance);
                r.SetMaterials(materials);
            }

            foreach(var o in outlineLayerObjects)
            {
                o.SetGameLayerRecursive(LayerMask.NameToLayer("Outline"));
            }
        }
    }

    protected override void OnCardDragEnded(Card card)
    {
        foreach (var r in outlineRenderers)
        {
            if (r.gameObject.layer != initialLayer)
            {
                List<Material> materials = new List<Material>(r.sharedMaterials);
                materials.Remove(outlineMaterialInstance);
                r.SetMaterials(materials);
            }
        }

        foreach (var o in outlineLayerObjects)
        {
            o.SetGameLayerRecursive(initialLayer);
        }
        collider.enabled = true;
    }

    private void OnCombatTimerChanged(int previous, int current)
    {
        if (roundManager.IsLastRound && current <= timeToPlayDeathTick)
        {
            StartDeathTickGraphics();
            roundManager.CombatRoundTimer.OnValueChanged -= OnCombatTimerChanged;
        }
    }

    private void StartDeathTickGraphics()
    {
        deathTickParticle.Play();
        StartCoroutine(DeathTickShrinkCoroutine());
    }

    private IEnumerator DeathTickShrinkCoroutine()
    {
        float timer = 0f;
        var shape = deathTickParticle.shape;
        float initialRadius = shape.radius;
        while (timer < timeToPlayDeathTick)
        {
            yield return null;
            timer += Time.deltaTime;
            shape.radius = Mathf.Lerp(initialRadius, deathTickDonutRadius, timer/timeToPlayDeathTick);
        }
        timer = 0f;
        while(timer < postDeathTickDonutShrinkTimer)
        {
            yield return null;
            timer += Time.deltaTime;
            shape.radius = Mathf.Lerp(deathTickDonutRadius, maxParticleDonutRadius, timer/postDeathTickDonutShrinkTimer); 
        }
    }
}
