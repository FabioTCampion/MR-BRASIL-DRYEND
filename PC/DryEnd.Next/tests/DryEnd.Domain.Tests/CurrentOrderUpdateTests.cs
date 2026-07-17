using DryEnd.Domain;

namespace DryEnd.Domain.Tests;

public sealed class CurrentOrderUpdateTests
{
    [Fact]
    public void ValidateStructure_AcceptsFivePapersAndFiveMeasuresPerOrder()
    {
        var update = CreateUpdate(["1", "2", "3", "4", "5"], [1, 2, 3, 4, 5]);

        update.ValidateStructure();
    }

    [Fact]
    public void ValidateStructure_RejectsIncompletePaperList()
    {
        var update = CreateUpdate(["1", "2"], [1, 2, 3, 4, 5]);

        Assert.Throws<ArgumentException>(update.ValidateStructure);
    }

    [Fact]
    public void ValidateStructure_RejectsIncompleteMeasureList()
    {
        var update = CreateUpdate(["1", "2", "3", "4", "5"], [1, 2]);

        Assert.Throws<ArgumentException>(update.ValidateStructure);
    }

    private static CurrentOrderUpdate CreateUpdate(
        IReadOnlyList<string> papers,
        IReadOnlyList<short> measures)
    {
        var channel = new OrderChannelUpdate(
            1, "Product", "Client", 0, 1, 1000, measures,
            100, 0, 100, 20, 0, 20, 0, 0);
        return new CurrentOrderUpdate(
            "2026-07-17", 1, 1, 1, "Composition", "B", 1800,
            papers, 1000, 0, 1000, 0.5f, false, false, channel, channel);
    }
}
