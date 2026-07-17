using DryEnd.Infrastructure.Ads;

namespace DryEnd.Infrastructure.Tests;

public sealed class AdsOptionsTests
{
    [Fact]
    public void Validate_AcceptsPlcRuntimeOnePort()
    {
        new AdsOptions { Port = 851 }.Validate();
    }

    [Fact]
    public void Validate_RejectsTwinCatSystemServicePort()
    {
        var options = new AdsOptions { Port = 10000 };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("forbidden", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
