namespace Orders.Domain;

public sealed record Order(string ClientOrderId, string Symbol, OrderSide Side, int Quantity, decimal Price);

public sealed record OrderDecision(
    string ClientOrderId,
    bool Accepted,
    string Message,
    decimal CurrentExposure);
