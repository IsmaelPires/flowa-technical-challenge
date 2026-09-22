using Orders.Domain;
using Xunit;

namespace Orders.Tests;

public sealed class ExposureLedgerTests
{
    [Fact]
    public void Buy_IncreasesExposure()
    {
        var ledger = new ExposureLedger();

        var result = ledger.TryApply(new("1", "PETR4", OrderSide.Buy, 100, 25.50m));

        Assert.True(result.Accepted);
        Assert.Equal(2_550m, result.CurrentExposure);
    }

    [Fact]
    public void Sell_DecreasesExposure()
    {
        var ledger = new ExposureLedger();
        ledger.TryApply(new("1", "VALE3", OrderSide.Buy, 100, 50m));

        var result = ledger.TryApply(new("2", "VALE3", OrderSide.Sell, 40, 50m));

        Assert.Equal(3_000m, result.CurrentExposure);
    }

    [Fact]
    public void OrderThatExceedsPositiveLimit_IsRejectedAndNotApplied()
    {
        var ledger = new ExposureLedger(1_000m);

        var result = ledger.TryApply(new("1", "VIIA4", OrderSide.Buy, 11, 100m));

        Assert.False(result.Accepted);
        Assert.Equal(0m, ledger.GetExposure("VIIA4"));
    }

    [Fact]
    public void OrderThatExceedsNegativeLimit_IsRejectedAndNotApplied()
    {
        var ledger = new ExposureLedger(1_000m);

        var result = ledger.TryApply(new("1", "PETR4", OrderSide.Sell, 11, 100m));

        Assert.False(result.Accepted);
        Assert.Equal(0m, ledger.GetExposure("PETR4"));
    }

    [Fact]
    public void ExposureAtExactLimit_IsAccepted()
    {
        var ledger = new ExposureLedger(1_000m);

        var result = ledger.TryApply(new("1", "PETR4", OrderSide.Buy, 10, 100m));

        Assert.True(result.Accepted);
        Assert.Equal(1_000m, result.CurrentExposure);
    }

    [Fact]
    public void DuplicateOrder_IsNotAccumulatedTwice()
    {
        var ledger = new ExposureLedger();
        var order = new Order("same-id", "PETR4", OrderSide.Buy, 10, 100m);

        var first = ledger.TryApply(order);
        var repeated = ledger.TryApply(order);

        Assert.True(first.Accepted);
        Assert.Equal(first, repeated);
        Assert.Equal(1_000m, ledger.GetExposure("PETR4"));
    }

    [Theory]
    [InlineData("INVALID", OrderSide.Buy, 1, 1)]
    [InlineData("PETR4", OrderSide.Buy, 0, 1)]
    [InlineData("PETR4", OrderSide.Buy, 100000, 1)]
    [InlineData("PETR4", OrderSide.Buy, 1, 0)]
    [InlineData("PETR4", OrderSide.Buy, 1, 1000)]
    [InlineData("PETR4", OrderSide.Buy, 1, 1.001)]
    public void InvalidOrder_IsRejected(string symbol, OrderSide side, int quantity, decimal price)
    {
        var result = new ExposureLedger().TryApply(new("1", symbol, side, quantity, price));

        Assert.False(result.Accepted);
    }
}
