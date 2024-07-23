using System;
using System.Collections.Generic;
using GameAnalyticsSDK;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using Utils;

using TMPro;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Authentication;
using System.Threading.Tasks;


/// <summary>
    /// An example of how to access and react to multiplay server events.
    /// </summary>
public class Example_ServerEvents : MonoBehaviour
{
    private MultiplayEventCallbacks m_MultiplayEventCallbacks;
    private IServerEvents m_ServerEvents;
    public bool isAllocated = false;

    /// <summary>
    /// This should be done early in the server's lifecycle, as you'll want to receive events as soon as possible.
    /// </summary>
    private async void Start()
    {
        // We must first prepare our callbacks like so:
        await Example_InitSDK();
#if UNITY_SERVER         
        m_MultiplayEventCallbacks = new MultiplayEventCallbacks();
        m_MultiplayEventCallbacks.Allocate += OnAllocate;
        m_MultiplayEventCallbacks.Deallocate += OnDeallocate;
        m_MultiplayEventCallbacks.Error += OnError;
        m_MultiplayEventCallbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;

        // We must then subscribe.
        m_ServerEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(m_MultiplayEventCallbacks);
#endif        
    }

    /// <summary>
    /// Handler for receiving the allocation multiplay server event.
    /// </summary>
    /// <param name="allocation">The allocation received from the event.</param>
    private void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log("Handling server allocated event");
        // Here is where you handle the allocation.
        // This is highly dependent on your game, however this would typically be some sort of setup process.
        // Whereby, you spawn NPCs, setup the map, log to a file, or otherwise prepare for players.
        // After you the allocation has been handled, you can then call ReadyServerForPlayersAsync()!
#if UNITY_SERVER  
        // TODO: May be there is a better way to notify GameManager that the server has been allocated
        // GameManager.Instance.IsAllocated = true;
        isAllocated = true;
#endif
    }

    /// <summary>
    /// Handler for receiving the deallocation multiplay server event.
    /// </summary>
    /// <param name="deallocation">The deallocation received from the event.</param>
    private void OnDeallocate(MultiplayDeallocation deallocation)
    {
        // Here is where you handle the deallocation.
        // This is highly dependent on your game, however this would typically be some sort of teardown process.
        // You might want to deactivate unnecessary NPCs, log to a file, or perform any other cleanup actions.
#if UNITY_SERVER        
        GameManager.Instance.IsDeallocated = true;
#endif
    }

    /// <summary>
    /// Handler for receiving the error multiplay server event.
    /// </summary>
    /// <param name="error">The error received from the event.</param>
    private void OnError(MultiplayError error)
    {
        // Here is where you handle the error.
        // This is highly dependent on your game. You can inspect the error by accessing the error.Reason and error.Detail fields.
        // You can change on the error.Reason field, log the error, or otherwise handle it as you need to.
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="state"></param>
    private void OnSubscriptionStateChanged(MultiplayServerSubscriptionState state)
    {
#if UNITY_SERVER
        switch (state)
        {
            case MultiplayServerSubscriptionState.Unsubscribed: /* The Server Events subscription has been unsubscribed from. */ break;
            case MultiplayServerSubscriptionState.Synced: /* The Server Events subscription is up to date and active. */ break;
            case MultiplayServerSubscriptionState.Unsynced: /* The Server Events subscription has fallen out of sync, the subscription tries to automatically recover. */ break;
            case MultiplayServerSubscriptionState.Error: /* The Server Events subscription has fallen into an errored state and won't recover automatically. */ break;
            case MultiplayServerSubscriptionState.Subscribing: /* The Server Events subscription is trying to sync. */ break;
        }
#endif
    }

    private async Task Example_InitSDK()
    {
        try
        {
            Debug.Log("ServerEvents UnityServices.InitializeAsync starting");
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync();
            Debug.Log("ServerEvents UnityServices.InitializeAsync finished");
        }
        catch (Exception e)
        {
            Debug.Log("ServerEvents UnityServices.InitializeAsync error: " + e);
        }
    }
}
