using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class IndicatorText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textUI;
    [SerializeField]
    private float animationDuration;
    [SerializeField]
    private float transparencyAnimationDuration;
    [SerializeField]
    private float moveDistance;
    
    private bool isAnimating;

    private void PlayAnimation(Action<IndicatorText> callback)
    {
        isAnimating = true;
        Sequence sequence = DOTween.Sequence()
                .Append(textUI.transform.DOMoveY(textUI.transform.position.y + moveDistance, animationDuration))
                .Insert(animationDuration - transparencyAnimationDuration, textUI.DOFade(0f, transparencyAnimationDuration))
                .OnComplete(() =>
                {
                    isAnimating = false;
                    callback?.Invoke(this);
                });
    }
    

    private void ShowHealthChange(int changedValue)
    {
        textUI.transform.localPosition = Vector3.zero;

        if (changedValue > 0)
        {
            textUI.color = GlobalGameSettings.Settings.GetEffectColors(CardEffectCategory.Healing).BackgroundColor;
            textUI.text = $"+{changedValue.ToString()}";
        }
        else
        {
            textUI.color = GlobalGameSettings.Settings.GetEffectColors(CardEffectCategory.Damage).BackgroundColor;
            textUI.text = changedValue.ToString();
        }
    }
    
    public void ShowIndicator(int changedValue, Action<IndicatorText> animationEndedCallback)
    {
        ShowHealthChange(changedValue);
        PlayAnimation(animationEndedCallback);
    }
}
