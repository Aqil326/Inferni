using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private TextMeshProUGUI descriptionText;
    
    public void SetToastInfo(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;
    }
}
