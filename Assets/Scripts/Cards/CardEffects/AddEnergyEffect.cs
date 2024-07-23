public class AddEnergyEffect : CardEffect
{
    public AddEnergyEffect(AddEnergyEffectData data) : base(data)
    {

    }

    public override string GetEffectCardText()
    {
        var data = GetData<AddEnergyEffectData>();
        string text = "";
        if (data.maxEnergy)
        {
            text = base.GetEffectCardText();
            return text;
        }

        text = base.GetEffectCardText();
        text = text.Replace("{X}", data.energyAmount.ToString());
        return text;
    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        var data = GetData<AddEnergyEffectData>();

        if (target is Character character)
        {
            EnergyPool[] pools = character.EnergyPools;
            int energyAmount = data.energyAmount;
            foreach(var p in pools)
            {
                if(data.maxEnergy)
                {
                    energyAmount = p.MaxEnergy;
                }

                if (data.useColor)
                {
                    if (data.energyColor == p.Color.Value && p.Energy.Value < p.MaxEnergy)
                    {
                        p.AddEnergy(energyAmount);
                        return;
                    }
                }
                else
                {
                    if(!p.IsEmpty)
                    {
                        p.AddEnergy(energyAmount);
                        return;
                    }
                }

            }
        }

        if(target is EnergyPool pool)
        {
            int energyAmount = data.energyAmount;
            if (data.maxEnergy)
            {
                energyAmount = pool.MaxEnergy;
            }

            if (!data.useColor || data.energyColor == pool.Color.Value)
            {
                pool.AddEnergy(energyAmount);
            }
        }
    }
}
