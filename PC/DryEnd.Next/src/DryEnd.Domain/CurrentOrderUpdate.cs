namespace DryEnd.Domain;

public sealed record CurrentOrderUpdate(
    string StartedAt,
    int TableId,
    int ProductionListNumber,
    short LevelSelector,
    string PaperComposition,
    string FluteType,
    short PaperWidth,
    IReadOnlyList<string> PaperLayers,
    float LinearMeters,
    float LinearMetersProduced,
    float LinearMetersRemaining,
    float ScorerHeightMm,
    bool InvertOrderLevel,
    bool InvertOrderSide,
    OrderChannelUpdate Order1,
    OrderChannelUpdate Order2)
{
    public void ValidateStructure()
    {
        if (PaperLayers.Count != 5)
            throw new ArgumentException("Exactly five paper layers are required.");

        Order1.ValidateStructure("Order 1");
        Order2.ValidateStructure("Order 2");
    }
}

public sealed record CurrentOrderPatch(
    CurrentOrderUpdate BaseSnapshot,
    CurrentOrderUpdate UpdatedOrder)
{
    public void ValidateStructure()
    {
        BaseSnapshot.ValidateStructure();
        UpdatedOrder.ValidateStructure();
    }
}

public sealed record OrderChannelUpdate(
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
    short ScrapCounter)
{
    public void ValidateStructure(string name)
    {
        if (SheetMeasures.Count != 5)
            throw new ArgumentException($"{name} requires exactly five sheet measures.");
    }
}
