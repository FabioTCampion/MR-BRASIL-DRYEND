namespace DryEnd.Domain;

public sealed class ProductionOrderRecord
{
    public int Id { get; set; }
    public int? ProductionSequence { get; set; }
    public int? ProductionState { get; set; }
    public int? MachineNotRunningTime { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? PaperComposition { get; set; }
    public string? FluteType { get; set; }
    public int? PaperWidth { get; set; }
    public string? Paper1 { get; set; }
    public string? Paper2 { get; set; }
    public string? Paper3 { get; set; }
    public string? Paper4 { get; set; }
    public string? Paper5 { get; set; }
    public string? ProductionListNumber { get; set; }
    public int? Order1Id { get; set; }
    public string? Order1Product { get; set; }
    public string? Order1Client { get; set; }
    public int? Order1SheetQuantity { get; set; }
    public int? Order1SheetType { get; set; }
    public int? Order1M1 { get; set; }
    public int? Order1M2 { get; set; }
    public int? Order1M3 { get; set; }
    public int? Order1M4 { get; set; }
    public int? Order1M5 { get; set; }
    public int? Order1SheetLength { get; set; }
    public int? Order1NumberOfCuts { get; set; }
    public int? Order1NumberOfCutsProduced { get; set; }
    public int? Order1PileQuantity { get; set; }
    public int? LevelSelector { get; set; }
    public int? Order2Id { get; set; }
    public string? Order2Product { get; set; }
    public string? Order2Client { get; set; }
    public int? Order2SheetQuantity { get; set; }
    public int? Order2SheetType { get; set; }
    public int? Order2M1 { get; set; }
    public int? Order2M2 { get; set; }
    public int? Order2M3 { get; set; }
    public int? Order2M4 { get; set; }
    public int? Order2M5 { get; set; }
    public int? Order2SheetLength { get; set; }
    public int? Order2NumberOfCuts { get; set; }
    public int? Order2NumberOfCutsProduced { get; set; }
    public int? Order2PileQuantity { get; set; }
}

public sealed record MachineSpeedRecord(DateTime DateTime, int MachineSpeed);

public sealed record DatabaseStatus(bool Configured, bool Available, string? Message);

public enum OrderSearchMode
{
    None,
    Client,
    Composition,
    ProductionList,
    WorkOrder,
    Product
}
