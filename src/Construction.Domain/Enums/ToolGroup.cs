namespace Construction.Domain.Enums;

/// <summary>
/// What kind of equipment a tool is for the planning screen, which draws one icon per kind. A kit (kofer) is shown as a
/// kofer whatever this says; everything else not listed here is <see cref="Other"/> and is only counted.
/// </summary>
public enum ToolGroup
{
    Other = 0,

    /// <summary>Battery set: drill, impact driver, two batteries and a charger.</summary>
    BatterySet = 1,

    /// <summary>Measuring set: multimeter, installation tester, laser, voltage detector.</summary>
    MeasuringSet = 2,

    /// <summary>Cable-pulling set: puller, spiral, wire, reel.</summary>
    CableSet = 3
}
