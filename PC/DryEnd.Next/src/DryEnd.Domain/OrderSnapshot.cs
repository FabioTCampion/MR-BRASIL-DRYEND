namespace DryEnd.Domain;

public sealed record OrderSnapshot(
    string StartedAt,
    int TableId,
    int ProductionListNumber,
    short LevelSelector,
    string PaperComposition,
    string FluteType,
    short PaperWidth,
    IReadOnlyList<string> PaperLayers,
    float LineSpeed,
    float LinearMeters,
    float LinearMetersProduced,
    float LinearMetersRemaining,
    float ScorerHeightMm,
    bool PlcWatchDog,
    bool AocRequest,
    bool ChangeOrderRequest,
    bool SaveSqlFinished,
    bool SaveSqlTimeout,
    bool InvertOrderLevel,
    bool InvertOrderSide,
    OrderChannelSnapshot Order1,
    OrderChannelSnapshot Order2,
    GeneratedOrderSnapshot GeneratedOrder);

public sealed record OrderChannelSnapshot(
    int Id,
    string Product,
    string Client,
    short SheetType,
    short SheetQuantity,
    short SheetLength,
    IReadOnlyList<short> SheetMeasures,
    int NumberOfCuts,
    int NumberOfCutsProduced,
    int NumberOfCutsRemaining,
    short PileQuantity,
    short PileQuantityProduced,
    short PileQuantityRemaining,
    short PileCounter,
    short ScrapCounter);

public sealed record GeneratedOrderSnapshot(
    short NumberOfKnives,
    short NumberOfScorers,
    short NumberOfSheets,
    float Order1Width,
    float Order2Width,
    float OrderTotalWidth,
    float FirstKnifePosition,
    float LastKnifePosition,
    IReadOnlyList<ToolReferenceSnapshot> Knives,
    IReadOnlyList<ToolReferenceSnapshot> Scorers,
    bool OrderNotOk,
    IReadOnlyList<int> KnivesOutOfRange,
    IReadOnlyList<int> ScorersOutOfRange);

public sealed record ToolReferenceSnapshot(int Index, bool Enabled, float PositionReferenceMm);
