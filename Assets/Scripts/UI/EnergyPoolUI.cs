using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class EnergyPoolUI : TargetableView
{
    [SerializeField]
    private TextMeshProUGUI energyAmountText;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private Image background;

    [SerializeField]
    private Image frame;

    [SerializeField]
    private Image timerImage;

    [SerializeField]
    private Image levelSliderImage;

    [SerializeField]
    private Image energyCountImage;

    [SerializeField]
    private Image LandImageBlack;

    [SerializeField]
    private Image LandImageBlue;

    [SerializeField]
    private Image LandImageGreen;

    [SerializeField]
    private Image LandImageRed;

    [SerializeField]
    private Image LandImageWhite;

    [SerializeField]
    private Vector3 mouseOverOffset = new Vector3(0, 0.2f, 0);

    [SerializeField]
    private TextMeshProUGUI promptText;
        
    public EnergyPool Pool { get; private set; }
    private CharacterManager characterManager;
    private Coroutine energyTimerCoroutine;
    private Vector3 initialBackgroundPosition;
    private CardColors localColor;

    [SerializeField]
    private SoundManager.Sound upgradeSound;
    [SerializeField]
    private SoundManager.Sound energyProducedSound;

    public void Init(EnergyPool pool)
    {
        characterManager = GameManager.GetManager<CharacterManager>();

        Pool = pool;
        SetTargetable(pool);
        Pool.Energy.OnValueChanged += UpdateVisuals;
        Pool.Level.OnValueChanged += UpdateVisuals;
        Pool.Energy.OnValueChanged += OnEnergy;
        Pool.Level.OnValueChanged += OnUpgrade;
        Pool.EnergyGenerationTimerRate.OnValueChanged += UpdateEnergyTimer;

        UpdateVisuals(0, 0);

        initialBackgroundPosition = background.transform.localPosition;

        promptText.text = "Drag card to start mana generation";
    }


    private void  UpdateEnergyTimer(float previous, float current)
    {
        timerImage.fillAmount = current;
    }

    private void OnEnergy(int previous, int current)
    {
        if (previous < current)
        {
            energyProducedSound.Play(false);
        }
    }

    private void OnUpgrade(int previous, int current)
    {
        upgradeSound.Play(false);
    }

    private void UpdateVisuals(int previous, int current)
    {
        if (Pool.IsEmpty)
        {
            levelText.enabled = false;
            energyAmountText.enabled = false;
            SetLandImage(CardColors.None);
        }
        else
        {
            levelText.enabled = true;
            energyAmountText.enabled = true;

            levelText.text = "Level " + (Pool.Level.Value);
            energyAmountText.text = $"{Pool.Energy.Value}/{Pool.MaxEnergy}";

            levelSliderImage.fillAmount = (float)(Pool.Level.Value) / (float)Pool.MaxLevel;

            if (Pool.Level.Value < Pool.MaxLevel)
            {
                promptText.text = "Drag card to upgrade.";
            }
            else
            {
                promptText.text = "Max level.";
            }

            //Set color on empty pool
            if (Pool.Color.Value != localColor)
            {
                frame.color = GlobalGameSettings.Settings.GetCardColor(Pool.Color.Value);
                energyCountImage.color = GlobalGameSettings.Settings.GetCardColor(Pool.Color.Value);
                SetLandImage(Pool.Color.Value);
            }

            ((TargetableView) this).TargetDeselected();
        }
    }

    private void SetLandImage(CardColors color)
    {
        localColor = color;
        LandImageBlack.enabled = false;
        LandImageBlue.enabled = false;
        LandImageGreen.enabled = false;
        LandImageRed.enabled = false;
        LandImageWhite.enabled = false;

        switch (color)
        {
            case CardColors.Black:
                LandImageBlack.enabled = true;
                break;

            case CardColors.Blue:
                LandImageBlue.enabled = true;
                break;
            case CardColors.Green:
                LandImageGreen.enabled = true;
                break;
            case CardColors.Red:
                LandImageRed.enabled = true;
                break;
            case CardColors.White:
                LandImageWhite.enabled = true;
                break;
        }

    }
  

    public override void TargetSelected(bool isValidTarget)
    {
        background.transform.localPosition = initialBackgroundPosition + mouseOverOffset;
    }

    public override void TargetDeselected()
    {
        background.transform.localPosition = initialBackgroundPosition;
    }

    public void OnPointerEnter()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_SELECTED_CLIENT, this);
    }

    public void OnPointerExit()
    {
        EventBus.TriggerEvent<TargetableView>(EventBusEnum.EventName.TARGET_DESELECTED_CLIENT, this);
    }

    protected override void OnCardDragStarted(Card card)
    {
        
    }

    protected override void OnCardDragEnded(Card card)
    {

    }
}
