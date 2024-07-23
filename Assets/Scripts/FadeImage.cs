using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeImage : MonoBehaviour
{
    private Image image;
    private Color startColor;
    private bool inFade = false;
    private float fadeTime = 0f;
    private float timeInFade = 0f;

    private void Start()
    {
        image = GetComponent<Image>();
        startColor = image.color;
    }

    public void DoFade(float seconds)
    {
        fadeTime = seconds;
        inFade = true;
        timeInFade = 0f;
    }

    private void Update()
    {
        if (!inFade) return;

        if (timeInFade < fadeTime)
        {
            timeInFade += Time.fixedDeltaTime;
            image.color = Color.Lerp(startColor, Color.black, timeInFade / fadeTime);

        }
        else
        {
            inFade = false;
        }
    }

}
