using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        LookAtCamera();
        EventBus.StartListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, LookAtCamera);
        EventBus.StartListening(EventBusEnum.EventName.MAIN_CAMERA_CHANGED_CLIENT, LookAtCamera);
    }

    public void LookAtCamera()
    {
        if(mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        transform.rotation = mainCamera.transform.rotation;
        //transform.LookAt(mainCamera.transform);
    }

    private void OnDestroy()
    {
        EventBus.StopListening(EventBusEnum.EventName.GAME_STARTED_CLIENT, LookAtCamera);
        EventBus.StopListening(EventBusEnum.EventName.MAIN_CAMERA_CHANGED_CLIENT, LookAtCamera);
    }

}
