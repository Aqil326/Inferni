using UnityEngine;
using System.Collections.Generic;

public class Particle : MonoBehaviour
{
    [SerializeField]
    private bool disableOnComplete = false;
    private List<ParticleSystem> particleSystems;


    private void OnEnable()
    {
        if (particleSystems == null) GetParticleSystems();
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, Unpause);
        EventBus.StartListening(EventBusEnum.EventName.END_ROUND_COMBAT_CLIENT, Pause);
    }

    private void OnDisable()
    {
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_CLIENT, Unpause);
        EventBus.StopListening(EventBusEnum.EventName.END_ROUND_COMBAT_CLIENT, Pause);
    }

    void Update()
    {
        if (disableOnComplete)
        {
            bool doDisable = true;

            foreach (ParticleSystem _particleSystem in particleSystems)
            {
                if (_particleSystem == null)
                {
                    Debug.LogError("Particle System missing from list");
                    continue; //Skip this.
                }

                if (_particleSystem.IsAlive())
                {
                    doDisable = false;
                    return;
                }
            }

            if (doDisable) gameObject.SetActive(false);
        }
    }

    public void Clear()
    {
        foreach (ParticleSystem _particleSystem in particleSystems)
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Particle System missing from list");
                continue; //Skip this.
            }

            _particleSystem.Clear(true);
        }
    }

    public void Pause()
    {
        foreach (ParticleSystem _particleSystem in particleSystems)
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Particle System missing from list");
                continue; //Skip this.
            }

            _particleSystem.Pause(true);
        }
    }
    
    public void Stop()
    {
        foreach (ParticleSystem _particleSystem in particleSystems)
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Particle System missing from list");
                continue; //Skip this.
            }

            _particleSystem.Stop(true);
        }
    }

    public void Unpause()
    {
        foreach (ParticleSystem _particleSystem in particleSystems)
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Particle System missing from list");
                continue; //Skip this.
            }

            _particleSystem.Play(true);
        }
    }

    public void Reset()
    {
        foreach (ParticleSystem _particleSystem in particleSystems)
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Particle System missing from list");
                continue; //Skip this.
            }

            _particleSystem.Clear(true);
            _particleSystem.Play(true);
        }
    }

    private void GetParticleSystems()
    {
        particleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>());
    }
}