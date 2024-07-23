using System.Collections.Generic;
using Newtonsoft.Json;

public class DraftPackCreatedInputData : CardListInputData
{
    private const string packId = "packId";

    [JsonProperty(packId)] public string PackId { get; private set; }
  

    public DraftPackCreatedInputData(string packId, string playerId, List<string> cardIds) : base(playerId, cardIds)
    {
        PackId = packId;
    }
}
