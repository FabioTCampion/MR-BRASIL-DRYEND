namespace DryEnd.Domain;

public sealed record OrderSnapshot(
    int TableId,
    int ProductionListNumber,
    short LevelSelector,
    string FluteType,
    float LineSpeed,
    bool PlcWatchDog);
