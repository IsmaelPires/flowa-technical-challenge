namespace Orders.Domain;

public static class OrderRules
{
    public static readonly IReadOnlySet<string> AllowedSymbols =
        new HashSet<string>(StringComparer.Ordinal) { "PETR4", "VALE3", "VIIA4" };

    public const int MaximumQuantity = 100_000;
    public const decimal MaximumPrice = 1_000m;

    public static string? Validate(string symbol, OrderSide side, int quantity, decimal price)
    {
        if (!AllowedSymbols.Contains(symbol))
            return "Símbolo inválido.";
        if (!Enum.IsDefined(side))
            return "Lado inválido.";
        if (quantity <= 0 || quantity >= MaximumQuantity)
            return "A quantidade deve ser positiva e menor que 100.000.";
        if (price <= 0 || price >= MaximumPrice)
            return "O preço deve ser positivo e menor que 1.000.";
        if (decimal.Round(price, 2) != price)
            return "O preço deve ser múltiplo de 0,01.";

        return null;
    }
}
