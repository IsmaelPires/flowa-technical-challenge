using Orders.Domain;
using QuickFix.Fields;
using Fix44 = QuickFix.FIX44;

namespace OrderAccumulator;

public sealed class FixOrderProcessor(ExposureLedger ledger)
{
    public Fix44.ExecutionReport Process(Fix44.NewOrderSingle request)
    {
        var order = MapOrder(request);
        var decision = ledger.TryApply(order);
        return CreateExecutionReport(request, decision);
    }

    private static Order MapOrder(Fix44.NewOrderSingle request)
    {
        var side = request.Side.Value switch
        {
            QuickFix.Fields.Side.BUY => OrderSide.Buy,
            QuickFix.Fields.Side.SELL => OrderSide.Sell,
            _ => (OrderSide)0
        };

        var fixQuantity = request.OrderQty.Value;
        var quantity = fixQuantity == decimal.Truncate(fixQuantity)
            && fixQuantity is > 0 and < OrderRules.MaximumQuantity
                ? decimal.ToInt32(fixQuantity)
                : 0;

        return new(
            request.ClOrdID.Value,
            request.Symbol.Value,
            side,
            quantity,
            request.Price.Value);
    }

    private static Fix44.ExecutionReport CreateExecutionReport(Fix44.NewOrderSingle request, OrderDecision decision)
    {
        var accepted = decision.Accepted;
        var report = new Fix44.ExecutionReport(
            new OrderID(Guid.NewGuid().ToString("N")),
            new ExecID(Guid.NewGuid().ToString("N")),
            new ExecType(accepted ? ExecType.NEW : ExecType.REJECTED),
            new OrdStatus(accepted ? OrdStatus.NEW : OrdStatus.REJECTED),
            request.Symbol,
            request.Side,
            new LeavesQty(accepted ? request.OrderQty.Value : 0m),
            new CumQty(0m),
            new AvgPx(0m));

        report.ClOrdID = request.ClOrdID;
        report.OrderQty = request.OrderQty;
        report.Price = request.Price;
        report.Text = new Text(decision.Message);
        report.SetField(new DecimalField(9000, decision.CurrentExposure));
        return report;
    }
}
