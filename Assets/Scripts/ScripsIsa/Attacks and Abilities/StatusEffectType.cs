// StatusEffectType.cs
using System;
using System.Collections.Generic;

public enum StatusEffectType
{
    None,
    Ralentizado,    // -20% de Movimiento
    Quemado,        // -10% de PV por 2 turnos
    Envenenado,     // -3% de PV por 4 turnos
    Noqueado,       // No actúa este turno
    Catalizador     // +15% Ataque (Patlee)
}

[Serializable]
public class StatusEffect
{
    public StatusEffectType Type;
    public int Duration;
}