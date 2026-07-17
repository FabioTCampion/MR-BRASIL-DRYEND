using DryEnd.Domain;

namespace DryEnd.Domain.Tests;

public sealed class NextOrderUpdateTests
{
    [Fact]
    public void FromDatabase_PreservesAllFiveMeasuresAndVariableText()
    {
        var order = CreateValidOrder();
        order.Order2Product = string.Empty;
        order.Order2Client = ";,POJ";

        var update = NextOrderUpdate.FromDatabase(order);

        Assert.Equal([164, 616, 164, 0, 0], update.Order1.SheetMeasures);
        Assert.Equal([208, 331, 208, 0, 0], update.Order2.SheetMeasures);
        Assert.Equal(string.Empty, update.Order2.Product);
        Assert.Equal(";,POJ", update.Order2.Client);
    }

    [Fact]
    public void FromDatabase_RejectsNullTextInActiveOrder()
    {
        var order = CreateValidOrder();
        order.Order1Client = null;

        Assert.Throws<ArgumentException>(() => NextOrderUpdate.FromDatabase(order));
    }

    [Fact]
    public void FromDatabase_RejectsNullMeasureInActiveOrder()
    {
        var order = CreateValidOrder();
        order.Order2M3 = null;

        Assert.Throws<ArgumentException>(() => NextOrderUpdate.FromDatabase(order));
    }

    [Fact]
    public void FromDatabase_AllowsNullFieldsForDisabledOrderAndWritesExplicitEmptyValues()
    {
        var order = CreateValidOrder();
        order.LevelSelector = 1;
        order.Order2Id = null;
        order.Order2Product = null;
        order.Order2Client = null;
        order.Order2M1 = null;

        var update = NextOrderUpdate.FromDatabase(order);

        Assert.Equal(NextOrderChannelUpdate.Empty, update.Order2);
    }

    [Fact]
    public void FromDatabase_RejectsNonNumericProductionList()
    {
        var order = CreateValidOrder();
        order.ProductionListNumber = "LIST-54";

        Assert.Throws<ArgumentException>(() => NextOrderUpdate.FromDatabase(order));
    }

    private static ProductionOrderRecord CreateValidOrder() => new()
    {
        Id = 131,
        ProductionSequence = 11,
        ProductionState = 0,
        PaperComposition = "N/A",
        FluteType = "B",
        PaperWidth = 1550,
        Paper1 = "100",
        Paper2 = "90",
        Paper3 = "90",
        Paper4 = "90",
        Paper5 = "100",
        ProductionListNumber = "54",
        LevelSelector = 3,
        Order1Id = 3000,
        Order1Product = "CX",
        Order1Client = "MR BRASIL",
        Order1SheetQuantity = 1,
        Order1SheetType = 1,
        Order1M1 = 164,
        Order1M2 = 616,
        Order1M3 = 164,
        Order1M4 = 0,
        Order1M5 = 0,
        Order1SheetLength = 1683,
        Order1NumberOfCuts = 14376,
        Order1PileQuantity = 40,
        Order2Id = 3000,
        Order2Product = string.Empty,
        Order2Client = ";,POJ",
        Order2SheetQuantity = 1,
        Order2SheetType = 1,
        Order2M1 = 208,
        Order2M2 = 331,
        Order2M3 = 208,
        Order2M4 = 0,
        Order2M5 = 0,
        Order2SheetLength = 2349,
        Order2NumberOfCuts = 10300,
        Order2PileQuantity = 350
    };
}
