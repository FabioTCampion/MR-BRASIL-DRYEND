using System.Globalization;

namespace DryEnd.Domain;

public sealed record NextOrderUpdate(
    int TableId,
    int ProductionListNumber,
    short LevelSelector,
    string PaperComposition,
    string FluteType,
    short PaperWidth,
    IReadOnlyList<string> PaperLayers,
    NextOrderChannelUpdate Order1,
    NextOrderChannelUpdate Order2)
{
    public static NextOrderUpdate FromDatabase(ProductionOrderRecord order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Id <= 0)
            throw new ArgumentException("Database order ID must be greater than zero.");
        if (order.ProductionSequence is null or <= 0)
            throw new ArgumentException("Production sequence must be greater than zero.");
        if (!int.TryParse(order.ProductionListNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var listNumber))
            throw new ArgumentException("Production list number must be a valid PLC integer.");
        if (listNumber <= 0)
            throw new ArgumentException("Production list number must be greater than zero.");
        if (order.LevelSelector is < 1 or > 3)
            throw new ArgumentException("Level selector must be 1, 2 or 3.");

        var level = checked((short)(order.LevelSelector ?? throw new ArgumentException("Level selector is required.")));
        var order1Enabled = level is 1 or 2 or 3;
        var order2Enabled = level == 3;

        return new NextOrderUpdate(
            order.Id,
            listNumber,
            level,
            RequiredText(order.PaperComposition, nameof(order.PaperComposition)),
            RequiredText(order.FluteType, nameof(order.FluteType)),
            RequiredShort(order.PaperWidth, 1, short.MaxValue, nameof(order.PaperWidth)),
            [
                RequiredText(order.Paper1, nameof(order.Paper1)),
                RequiredText(order.Paper2, nameof(order.Paper2)),
                RequiredText(order.Paper3, nameof(order.Paper3)),
                RequiredText(order.Paper4, nameof(order.Paper4)),
                RequiredText(order.Paper5, nameof(order.Paper5))
            ],
            order1Enabled ? CreateOrder1(order) : NextOrderChannelUpdate.Empty,
            order2Enabled ? CreateOrder2(order) : NextOrderChannelUpdate.Empty);
    }

    public void ValidateStructure()
    {
        if (TableId <= 0)
            throw new ArgumentException("Database order ID must be greater than zero.");
        if (ProductionListNumber <= 0)
            throw new ArgumentException("Production list number must be greater than zero.");
        if (LevelSelector is < 1 or > 3)
            throw new ArgumentException("Level selector must be 1, 2 or 3.");
        if (PaperLayers.Count != 5)
            throw new ArgumentException("Exactly five paper layers are required.");
        if (PaperLayers.Any(layer => layer is null))
            throw new ArgumentException("Paper layers cannot contain null values.");
        if (PaperComposition is null || FluteType is null)
            throw new ArgumentException("Paper information cannot be null.");

        Order1.ValidateStructure();
        Order2.ValidateStructure();
    }

    public bool Matches(OrderSnapshot snapshot) =>
        snapshot.TableId == TableId &&
        snapshot.ProductionListNumber == ProductionListNumber &&
        snapshot.LevelSelector == LevelSelector &&
        string.Equals(snapshot.PaperComposition, PaperComposition, StringComparison.Ordinal) &&
        string.Equals(snapshot.FluteType, FluteType, StringComparison.Ordinal) &&
        snapshot.PaperWidth == PaperWidth &&
        snapshot.PaperLayers.SequenceEqual(PaperLayers, StringComparer.Ordinal) &&
        Order1.Matches(snapshot.Order1) &&
        Order2.Matches(snapshot.Order2);

    private static NextOrderChannelUpdate CreateOrder1(ProductionOrderRecord order) =>
        CreateChannel(
            "Order 1",
            order.Order1Id,
            order.Order1Product,
            order.Order1Client,
            order.Order1SheetType,
            order.Order1SheetQuantity,
            order.Order1SheetLength,
            [order.Order1M1, order.Order1M2, order.Order1M3, order.Order1M4, order.Order1M5],
            order.Order1NumberOfCuts,
            order.Order1PileQuantity);

    private static NextOrderChannelUpdate CreateOrder2(ProductionOrderRecord order) =>
        CreateChannel(
            "Order 2",
            order.Order2Id,
            order.Order2Product,
            order.Order2Client,
            order.Order2SheetType,
            order.Order2SheetQuantity,
            order.Order2SheetLength,
            [order.Order2M1, order.Order2M2, order.Order2M3, order.Order2M4, order.Order2M5],
            order.Order2NumberOfCuts,
            order.Order2PileQuantity);

    private static NextOrderChannelUpdate CreateChannel(
        string name,
        int? id,
        string? product,
        string? client,
        int? sheetType,
        int? sheetQuantity,
        int? sheetLength,
        IReadOnlyList<int?> measures,
        int? numberOfCuts,
        int? pileQuantity)
    {
        if (id is null or <= 0)
            throw new ArgumentException($"{name} ID must be greater than zero.");
        if (sheetType is null or < 0 or > 2)
            throw new ArgumentException($"{name} sheet type must be 0, 1 or 2.");
        if (numberOfCuts is null or <= 0)
            throw new ArgumentException($"{name} number of cuts must be greater than zero.");

        var activeMeasureCount = sheetType.Value switch { 0 => 1, 1 => 3, _ => 5 };
        var convertedMeasures = measures
            .Select((value, index) => RequiredShort(
                index >= activeMeasureCount ? value ?? 0 : value,
                0,
                short.MaxValue,
                $"{name} M{index + 1}"))
            .ToArray();
        if (convertedMeasures.Take(activeMeasureCount).Any(value => value <= 0))
            throw new ArgumentException($"{name} active measures must be greater than zero.");

        return new NextOrderChannelUpdate(
            id.Value,
            RequiredText(product, $"{name} product"),
            RequiredText(client, $"{name} client"),
            checked((short)sheetType.Value),
            RequiredShort(sheetQuantity, 1, short.MaxValue, $"{name} sheet quantity"),
            RequiredShort(sheetLength, 450, 2800, $"{name} sheet length"),
            convertedMeasures,
            numberOfCuts.Value,
            RequiredShort(pileQuantity, 1, short.MaxValue, $"{name} pile quantity"));
    }

    private static string RequiredText(string? value, string name) =>
        value ?? throw new ArgumentException($"{name} cannot be null.");

    private static short RequiredShort(int? value, int minimum, int maximum, string name)
    {
        if (value is null || value < minimum || value > maximum)
            throw new ArgumentException($"{name} must be between {minimum} and {maximum}.");
        return checked((short)value.Value);
    }
}

public sealed record NextOrderChannelUpdate(
    int Id,
    string Product,
    string Client,
    short SheetType,
    short SheetQuantity,
    short SheetLength,
    IReadOnlyList<short> SheetMeasures,
    int NumberOfCuts,
    short PileQuantity)
{
    public static NextOrderChannelUpdate Empty { get; } =
        new(0, string.Empty, string.Empty, 0, 0, 0, [0, 0, 0, 0, 0], 0, 0);

    public void ValidateStructure()
    {
        if (Product is null || Client is null)
            throw new ArgumentException("Order product and client cannot be null.");
        if (SheetMeasures.Count != 5)
            throw new ArgumentException("Exactly five sheet measures are required per order.");
    }

    public bool Matches(OrderChannelSnapshot snapshot) =>
        snapshot.Id == Id &&
        string.Equals(snapshot.Product, Product, StringComparison.Ordinal) &&
        string.Equals(snapshot.Client, Client, StringComparison.Ordinal) &&
        snapshot.SheetType == SheetType &&
        snapshot.SheetQuantity == SheetQuantity &&
        snapshot.SheetLength == SheetLength &&
        snapshot.SheetMeasures.SequenceEqual(SheetMeasures) &&
        snapshot.NumberOfCuts == NumberOfCuts &&
        snapshot.PileQuantity == PileQuantity;
}
