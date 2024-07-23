using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class CameraManager : MonoBehaviour
{
    public float shakeDuration = 1.0f;       // Duration of the shake effect
    public float shakeMagnitude = 0.25f;      // Initial shake intensity
    public float dampingSpeed = 1.0f;        // Speed at which the shake effect decays

    [SerializeField]
    private Volume postProcessVolume;

    [SerializeField, Range(-100, 100)]
    private float draggingColorSaturation;

    private List<Renderer> outlineObjects = new List<Renderer>();
    
    private float initialColorSaturation;

    Vector3 initialPosition;                 // Initial position of the camera to return after shake
    Character playerCharacter;
    ColorAdjustments colorGrading;

    private void HealthChanged(int oldHealth, int newHealth)
    {
        int delta = newHealth - oldHealth;
        if (delta < 0) ScreenShake();
    }

    public void Init(Character playerCharacter)
    {
        this.playerCharacter = playerCharacter;
        playerCharacter.Health.OnValueChanged += HealthChanged;
        initialPosition = transform.localPosition;

        
        postProcessVolume.profile.TryGet(out colorGrading);
        initialColorSaturation = colorGrading.saturation.value;

        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, DesaturateColor);
        EventBus.StartListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, SaturateColor);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, DesaturateColor);
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, SaturateColor);
    }

    private void DesaturateColor(Card card)
    {
        DesaturateColor();
    }

    private void SaturateColor(Card card)
    {
        SaturateColor();
    }

    private void DesaturateColor()
    {
        colorGrading.saturation.value = draggingColorSaturation;
    }

    private void SaturateColor()
    {
        colorGrading.saturation.value = initialColorSaturation;
    }

    private void ScreenShake()
    {
        // Starts the coroutine to shake the camera
        StopAllCoroutines(); // Stop any existing shakes
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Calculate current shake magnitude
            float currentMagnitude = shakeMagnitude * (1 - (elapsed / shakeDuration));

            // Generate random shake position
            Vector3 shakePosition = initialPosition + Random.insideUnitSphere * currentMagnitude;

            // Apply shake position to the camera
            transform.localPosition = shakePosition;

            // Increment elapsed time
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame before continuing the loop
        }

        // Return the camera to its initial position
        transform.localPosition = initialPosition;
    }

    private void OnDisable()
    {
        if (playerCharacter)
        {
            GameManager.GetManager<CharacterManager>().PlayerCharacter.Health.OnValueChanged
                -= HealthChanged;
        }

        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_STARTED_CLIENT, DesaturateColor);
        EventBus.StopListening<Card>(EventBusEnum.EventName.CARD_DRAG_ENDED_CLIENT, SaturateColor);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_DRAFT_CLIENT, DesaturateColor);
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, SaturateColor);
    }
}