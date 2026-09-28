namespace Construction.Domain.Enums;

/// <summary>What kind of toll obligation a <see cref="Entities.VehicleToll"/> row represents.</summary>
public enum VehicleTollType
{
    Vignette = 1,
    Tunnel = 2,
    Passage = 3
}
