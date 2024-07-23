using Newtonsoft.Json;

public class HealthChangeInputData
{
    private const string playerId = "playerId";
    private const string health = "health";

    [JsonProperty(playerId)] public string PlayerId { get; private set; }
    [JsonProperty(health)] public int Health { get; private set; }

    public HealthChangeInputData(string playerId, int health)
    {
        PlayerId = playerId;
        Health = health;
    }
}

