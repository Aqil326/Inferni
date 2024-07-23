using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToastManager : BaseManager
{
    [SerializeField]
    private ToastUI toastPrefab;
    [SerializeField]
    public Canvas canvas;       
    
    public void ShowToast(string title, string message, float duration)
    {
        var toastInstance = Instantiate(toastPrefab, canvas.transform);
        toastInstance.SetToastInfo(title, message);
        StartCoroutine(HideToast(toastInstance, duration));
    }

    private IEnumerator HideToast(ToastUI toastInstance, float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(toastInstance.gameObject);
    }
}
