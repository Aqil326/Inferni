using System.Collections.Generic;
using Newtonsoft.Json;

public class CardListInputData
{
    private const string playerId = "playerId";
    private const string cardIds = "cardIds";

    [JsonProperty(playerId)] public string PlayerId { get; private set; }
    [JsonProperty(cardIds)] public List<string> CardIds { get; private set; }

    public CardListInputData(string playerId, List<string> cardIds)
    {
        PlayerId = playerId;
        CardIds = cardIds;
    }
}
