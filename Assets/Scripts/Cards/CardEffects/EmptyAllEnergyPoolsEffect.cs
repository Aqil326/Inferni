
public class EmptyAllEnergyPoolsEffect : CardEffect
{
    public EmptyAllEnergyPoolsEffect(EmptyAllEnergyPoolsEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            foreach(var p in character.EnergyPools)
            {
                p.DeductEnergy(p.Energy.Value);
            }
        }

        if(target is EnergyPool pool)
        {
            foreach (var p in pool.Owner.EnergyPools)
            {
                p.DeductEnergy(p.Energy.Value);
            }
        }
    }
}
