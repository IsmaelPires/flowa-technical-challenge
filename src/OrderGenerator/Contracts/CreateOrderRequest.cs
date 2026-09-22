using System.ComponentModel.DataAnnotations;
using Orders.Domain;

namespace OrderGenerator.Contracts;

public sealed class CreateOrderRequest : IValidatableObject
{
    [Required]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public string Side { get; init; } = string.Empty;

    public int Quantity { get; init; }
    public decimal Price { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.TryParse<OrderSide>(Side, true, out var side))
        {
            yield return new("O lado deve ser Buy ou Sell.", [nameof(Side)]);
            yield break;
        }

        var error = OrderRules.Validate(Symbol, side, Quantity, Price);
        if (error is not null)
            yield return new(error, [nameof(Symbol), nameof(Side), nameof(Quantity), nameof(Price)]);
    }
}

public sealed record OrderResponse(
    string ClientOrderId,
    string Status,
    string Message,
    decimal? Exposure);
