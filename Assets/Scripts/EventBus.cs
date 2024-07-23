using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventBusEnum
{
    public enum EventName
    {
        START_CASTING_CLIENT,
        PROJECTILE_SHOT_CLIENT,
        PROJECTILE_SHOT_SERVER,
        CARD_INSPECTED_CLIENT,
        CLEAR_CARD_INSPECTED_CLIENT,
        TARGET_SELECTED_CLIENT,
        TARGET_DESELECTED_CLIENT,
        START_ROUND_COMBAT_CLIENT,
        START_ROUND_COMBAT_SERVER,
        START_ROUND_DRAFT_CLIENT,
        START_ROUND_DRAFT_SERVER,
        END_ROUND_COMBAT_CLIENT,
        END_ROUND_COMBAT_SERVER,
        END_ROUND_DRAFT_CLIENT,
        END_ROUND_DRAFT_SERVER,
        DRAFT_PACK_PASS_CLIENT,
        DRAFT_PACK_COMPLETE_CLIENT,
        DRAFT_PLAYER_SELECT_CLIENT,
        DRAFT_TIMER_UPDATED_CLIENT,
        GAME_STARTED_CLIENT,
        PROJECTILE_TARGET_CHANGED_CLIENT,
        SERVER_DISCONNECTED_CLIENT,
        SHOW_TOOLTIP_CLIENT,
        CLEAR_TOOLTIP_CLIENT,
        CARD_ADDED_TO_HAND_WITH_POSITION_SERVER,
        CARD_ADDED_TO_DECK_WITH_POSITION_SERVER,
        RESET_MAIN_MENU,
        SERVER_REJECTED_CLIENT,
        REJECT_MAIN_MENU_CLIENT,
        SERVER_DISCONNECT_CLIENT,
        CLIENT_JOIN_LOBBY,
        SERVER_FOUND_FOR_LOBBY,
        CLIENT_CREATE_MATCHMAKER_TICKET_SUCCESS,
        GAME_END_SERVER,
        CARD_DRAG_STARTED_CLIENT,
        CARD_DRAG_ENDED_CLIENT,
        START_DEATH_TICK_CLIENT,
        MAIN_CAMERA_CHANGED_CLIENT,
        CHARACTERS_INITIALIZED_CLIENT,
        DEATH_TICK_APPLIED_SERVER,
        CHARACTER_KILLED_NEMESIS_CLIENT,
    }

    public enum ValueName
    {

    }
}

public class EventBus : MonoBehaviour
{
    Hashtable eventHash = new Hashtable();

    private static EventBus eventBus;

    public static EventBus Instance
    {
        get
        {
            if (!eventBus)
            {
                var go = new GameObject("Event Bus");
                eventBus = go.AddComponent<EventBus>();
                eventBus.Init();
            }
            return eventBus;
        }
    }

    public static void StartListening<T>(EventBusEnum.EventName eventName, Action<T> listener)
    {
        List<Action<T>> thisEvent = null;

        string key = GetKey<T>(eventName);

        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action<T>>)Instance.eventHash[key];
            thisEvent.Add(listener);
            Instance.eventHash[eventName] = thisEvent;
        }
        else
        {
            thisEvent = new List<Action<T>>();
            thisEvent.Add(listener);
            Instance.eventHash.Add(key, thisEvent);

        }
    }

    public static void StopListening<T>(EventBusEnum.EventName eventName, Action<T> listener)
    {
        if (eventBus == null)
        {
            return;
        }

        List<Action<T>> thisEvent = null;
        string key = GetKey<T>(eventName);
        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action<T>>)Instance.eventHash[key];
            thisEvent.Remove(listener);
            Instance.eventHash[eventName] = thisEvent;
        }
    }

    public static void TriggerEvent<T>(EventBusEnum.EventName eventName, T val)
    {
        List<Action<T>> thisEvent = null;
        string key = GetKey<T>(eventName);
        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action<T>>)Instance.eventHash[key];
            foreach (var a in thisEvent)
            {
                a.Invoke(val);
            }
        }
    }

    public static void StartListening(EventBusEnum.EventName eventName, Action listener)
    {
        List<Action> thisEvent = null;

        string key = eventName.ToString();

        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action>)Instance.eventHash[key];
            thisEvent.Add(listener);
            Instance.eventHash[eventName] = thisEvent;
        }
        else
        {
            thisEvent = new List<Action>();
            thisEvent.Add(listener);
            Instance.eventHash.Add(key, thisEvent);
        }
    }

    public static void StopListening(EventBusEnum.EventName eventName, Action listener)
    {
        if (eventBus == null)
        {
            return;
        }

        List<Action> thisEvent = null;
        string key = eventName.ToString();
        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action>)Instance.eventHash[key];
            thisEvent.Remove(listener);
            Instance.eventHash[eventName] = thisEvent;
        }
    }

    public static void AddValueDelegate<T>(EventBusEnum.ValueName valueName, Func<T> del)
    {
        string key = GetValueKey<T>(valueName);
        if (Instance.eventHash.ContainsKey(key))
        {
            Debug.Log($"Delegate already set for key {key}. Overriding it");
        }
        Instance.eventHash[key] = del;
    }

    public static void RemoveValueDelegate<T>(EventBusEnum.ValueName valueName)
    {
        string key = GetValueKey<T>(valueName);
        if (!Instance.eventHash.ContainsKey(key))
        {
            Debug.LogError($"Delegate not set for key {key}.");
            return;
        }
        Instance.eventHash.Remove(key);
    }

    public static T GetValue<T>(EventBusEnum.ValueName valueName)
    {
        string key = GetValueKey<T>(valueName);
        if (Instance.eventHash.ContainsKey(key))
        {
            var thisEvent = (Func<T>)Instance.eventHash[key];
            return thisEvent.Invoke();
        }
        Debug.LogError($"No Value delegate set for value {valueName}");
        return default;
    }

    public static void TriggerEvent(EventBusEnum.EventName eventName)
    {
        List<Action> thisEvent = null;
        string key = eventName.ToString();
        if (Instance.eventHash.ContainsKey(key))
        {
            thisEvent = (List<Action>)Instance.eventHash[key];
            foreach (var e in thisEvent)
            {
                e.Invoke();
            }
        }
    }

    private static string GetKey<T>(EventBusEnum.EventName eventName)
    {
        Type type = typeof(T);
        string key = type.ToString() + eventName.ToString();
        return key;
    }

    private static string GetValueKey<T>(EventBusEnum.ValueName valueName)
    {
        Type type = typeof(T);
        string key = type.ToString() + valueName.ToString();
        return key;
    }

    private void Init()
    {
        DontDestroyOnLoad(gameObject);
        if (eventBus.eventHash == null)
        {
            eventBus.eventHash = new Hashtable();
        }
    }
}
