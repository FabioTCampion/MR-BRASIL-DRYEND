namespace DryEnd.Domain;

public sealed record PlcDataSnapshot(
    OrderSnapshot CurrentOrder,
    OrderSnapshot NextOrder,
    DateTimeOffset CapturedAtUtc);
