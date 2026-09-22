using OrderAccumulator;
using Orders.Domain;
using QuickFix.Fields;
using Xunit;
using Fix44 = QuickFix.FIX44;

namespace Orders.Tests;

public sealed class FixOrderProcessorTests
{
    [Fact]
    public void AcceptedOrder_ReturnsNewExecutionReport()
    {
        var processor = new FixOrderProcessor(new ExposureLedger());

        var report = processor.Process(CreateOrder("order-1", Side.BUY, 100m, 25.50m));

        Assert.Equal(ExecType.NEW, report.ExecType.Value);
        Assert.Equal(OrdStatus.NEW, report.OrdStatus.Value);
        Assert.Equal("order-1", report.ClOrdID.Value);
        Assert.Equal(2_550m, report.GetDecimal(9000));
    }

    [Fact]
    public void OrderAboveLimit_ReturnsRejectedAndPreservesExposure()
    {
        var ledger = new ExposureLedger(1_000m);
        var processor = new FixOrderProcessor(ledger);

        var report = processor.Process(CreateOrder("order-1", Side.BUY, 11m, 100m));

        Assert.Equal(ExecType.REJECTED, report.ExecType.Value);
        Assert.Equal(OrdStatus.REJECTED, report.OrdStatus.Value);
        Assert.Equal(0m, ledger.GetExposure("PETR4"));
    }

    [Theory]
    [InlineData('3', 10)]
    [InlineData(Side.BUY, 10.5)]
    public void InvalidFixFields_ReturnRejected(char side, decimal quantity)
    {
        var processor = new FixOrderProcessor(new ExposureLedger());

        var report = processor.Process(CreateOrder("order-1", side, quantity, 10m));

        Assert.Equal(ExecType.REJECTED, report.ExecType.Value);
        Assert.Equal(0m, report.GetDecimal(9000));
    }

    [Fact]
    public void RepeatedClientOrderId_IsIdempotent()
    {
        var ledger = new ExposureLedger();
        var processor = new FixOrderProcessor(ledger);
        var order = CreateOrder("order-1", Side.BUY, 100m, 10m);

        var first = processor.Process(order);
        var repeated = processor.Process(order);

        Assert.Equal(ExecType.NEW, first.ExecType.Value);
        Assert.Equal(ExecType.NEW, repeated.ExecType.Value);
        Assert.Equal(1_000m, ledger.GetExposure("PETR4"));
    }

    [Fact]
    public void ReusedClientOrderIdWithDifferentPayload_IsRejected()
    {
        var ledger = new ExposureLedger();
        var processor = new FixOrderProcessor(ledger);
        processor.Process(CreateOrder("order-1", Side.BUY, 100m, 10m));

        var report = processor.Process(CreateOrder("order-1", Side.BUY, 200m, 10m));

        Assert.Equal(ExecType.REJECTED, report.ExecType.Value);
        Assert.Equal("ClOrdID já utilizado por outra ordem.", report.Text.Value);
        Assert.Equal(1_000m, ledger.GetExposure("PETR4"));
    }

    private static Fix44.NewOrderSingle CreateOrder(string id, char side, decimal quantity, decimal price)
    {
        var message = new Fix44.NewOrderSingle(
            new ClOrdID(id),
            new Symbol("PETR4"),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));
        message.OrderQty = new OrderQty(quantity);
        message.Price = new Price(price);
        return message;
    }
}
