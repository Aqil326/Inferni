public enum CardColors
{
    None,
    Blue,
    Red,
    Green,
    Black,
    White,
}

public enum CardTarget
{
    None,
    Any,
    Self,
    AnyOpponent,
    AnyPlayer,
    AllOpponents,
    All,
    Spell,
    OpponentsSpell,
    EnergyPool,
    World,
    Teammate,
    CardInHand,
    EmptyCharmSlot,
    DownedPlayer,
    Charm,
    SpellInCast,
}

public enum CardEffectTarget
{
    None,
    CardTargets,
    Owner,
    AllOpponents,
    All,
    AllProjectiles,
    World,
    AllTeammates
}

public enum CardTypes
{
    Spell,
    Energy
}

public enum CardSubTypes
{
    Attack,
    Effect
}

public enum CardXPreviewType
{
    Initial, // X is shown on Card when card is in Hand / in Draft / in Casting
    Flying, // X is shown on Card when hovering on projectile / Incoming UI icon
    TrySetTarget, // X is shown on Card when drag the card on the target player's head
}
