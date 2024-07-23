using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class AddCardToDeckInputData
{
    private const string playerId = "playerId";
    private const string cardId = "cardId";
    private const string draftPackId = "draftPackId";

    [JsonProperty(playerId)] public string PlayerId { get; private set; }
    [JsonProperty(draftPackId)] public string DraftPackId { get; private set; }
    [JsonProperty(cardId)] public string CardId { get; private set; }

    public AddCardToDeckInputData(string playerId, string cardId, string draftPackId = "")
    {
        PlayerId = playerId;
        CardId = cardId;
        DraftPackId = draftPackId;
    }
}


