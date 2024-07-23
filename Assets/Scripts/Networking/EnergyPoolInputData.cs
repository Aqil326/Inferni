using Newtonsoft.Json;

public class EnergyPoolInputData
{
    private const string playerId = "playerId";
    private const string index = "index";
    private const string color = "color";
    private const string energyAmount = "energyAmount";

    [JsonProperty(playerId)] public string PlayerId { get; private set; }
    [JsonProperty(index)] public int Index { get; private set; }
    [JsonProperty(color)] public CardColors Color { get; private set; }
    [JsonProperty(energyAmount)] public int EnergyAmount { get; private set; }

    public EnergyPoolInputData(string playerId, int index, CardColors color, int energyAmount)
    {
        PlayerId = playerId;
        Index = index;
        Color = color;
        EnergyAmount = energyAmount;
    }
}