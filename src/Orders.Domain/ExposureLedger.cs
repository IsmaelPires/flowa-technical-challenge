namespace Orders.Domain;

public sealed class ExposureLedger(decimal exposureLimit = ExposureLedger.DefaultExposureLimit)
{
    public const decimal DefaultExposureLimit = 100_000_000m;
    private readonly object _sync = new();
    private readonly Dictionary<string, decimal> _exposures =
        OrderRules.AllowedSymbols.ToDictionary(symbol => symbol, _ => 0m, StringComparer.Ordinal);
    private readonly Dictionary<string, (Order Order, OrderDecision Decision)> _processedOrders =
        new(StringComparer.Ordinal);

    public OrderDecision TryApply(Order order)
    {
        var validationError = OrderRules.Validate(order.Symbol, order.Side, order.Quantity, order.Price);
        if (validationError is not null)
            return new(order.ClientOrderId, false, validationError, GetExposure(order.Symbol));

        lock (_sync)
        {
            var current = _exposures[order.Symbol];

            if (_processedOrders.TryGetValue(order.ClientOrderId, out var processed))
            {
                return processed.Order == order
                    ? processed.Decision
                    : new(order.ClientOrderId, false, "ClOrdID já utilizado por outra ordem.", current);
            }

            var signedValue = order.Price * order.Quantity * (order.Side == OrderSide.Buy ? 1m : -1m);
            var projected = current + signedValue;

            if (Math.Abs(projected) > exposureLimit)
            {
                var rejection = new OrderDecision(
                    order.ClientOrderId,
                    false,
                    $"Limite de exposição de {exposureLimit:C} excedido para {order.Symbol}.",
                    current);
                _processedOrders.Add(order.ClientOrderId, (order, rejection));
                return rejection;
            }

            _exposures[order.Symbol] = projected;
            var acceptance = new OrderDecision(order.ClientOrderId, true, "Ordem aceita.", projected);
            _processedOrders.Add(order.ClientOrderId, (order, acceptance));
            return acceptance;
        }
    }

    public decimal GetExposure(string symbol)
    {
        lock (_sync)
            return _exposures.GetValueOrDefault(symbol);
    }

    public IReadOnlyDictionary<string, decimal> Snapshot()
    {
        lock (_sync)
            return new Dictionary<string, decimal>(_exposures, StringComparer.Ordinal);
    }
}
