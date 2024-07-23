using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DraftPackIcon : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI countText;
    [SerializeField]
    private Vector3 packStackOffset;

    private bool moving = false;
    private Vector3 startPostition;
    private Vector3 endPosition;
    private float currentTime;

    public DraftPackData DraftPack { get; private set; }

    public void Initialise(DraftPackData newPack)
    {
        DraftPack = newPack;
    }

    public void PassPack(DraftPackData data)
    {
        if(DraftPack.ID != data.ID)
        {
            return;
        }

        DraftPack = data;
        moving = true;
        currentTime = 0;
        countText.text = "x" + (DraftPack.CardCount + DraftPack.CharmCount).ToString();
        startPostition = transform.position;
        endPosition = DraftPack.EndPosition;
    }


    private void Update()
    {
        if (!moving) return;

        //Lerp towards location
        currentTime += Time.deltaTime;
        float fractionOfJourney = currentTime / GlobalGameSettings.Settings.DraftPackTravelDuration;
        transform.position = Vector3.Lerp(startPostition, endPosition, fractionOfJourney);

        if (fractionOfJourney >= 1f)
        {
            ReachedEnd();
        }
    }

    private void ReachedEnd()
    {
        transform.position = endPosition;
        moving = false;
    }
}
