using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class DraftPackInputData
{
    private const string draftPackId = "draftPackId";

    private const string previousOwnerId = "previousOwnerId";

    private const string currentOwnerId = "currentOwnerId";

    [JsonProperty(draftPackId)] public string DraftPackID { get; private set; }
    [JsonProperty(previousOwnerId)] public string PreviousOwnerID { get; private set; }
    [JsonProperty(currentOwnerId)] public string CurrentOwnerID { get; private set; }

    public DraftPackInputData(string packId, string previousOwnerId, string currentOwnerId)
    {
        DraftPackID = packId;
        PreviousOwnerID = previousOwnerId;
        CurrentOwnerID = currentOwnerId;
    }
}
