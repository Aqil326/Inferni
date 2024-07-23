using Newtonsoft.Json;

public class InputData
{
    private const string playerId = "playerId";
    private const string targetId = "targetId";
    private const string cardId = "cardId";
    private const string cardHandIndex = "cardHandIndex";
    private const string timeInHand = "timeInHand";

    [JsonProperty(playerId)] public string PlayerId { get; private set; }
    [JsonProperty(targetId)] public string TargetId { get; private set; }
    [JsonProperty(cardId)] public string CardId { get; private set; }
    [JsonProperty(cardHandIndex)] public int CardHandIndex { get; private set; }
    [JsonProperty(timeInHand)] public float TimeInHand { get; private set; }

    public InputData(string playerId, string targetId, string cardId, int cardHandIndex = -1, float timeInHand = 0)
    {
        PlayerId = playerId;
        TargetId = targetId;
        CardId = cardId;
        CardHandIndex = cardHandIndex;
        TimeInHand = timeInHand;
    }
}
