public class IncreaseEnergyPoolLevelCardEffect : CardEffect
{
    public IncreaseEnergyPoolLevelCardEffect(IncreaseEnergyPoolLevelCardEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if(target is EnergyPool energyPool)
        {
            energyPool.IncreaseLevel(GetData<IncreaseEnergyPoolLevelCardEffectData>().numberOfLevels);
        }
    }
}
