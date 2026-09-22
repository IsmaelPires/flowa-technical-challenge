using OrderGenerator.Contracts;
using OrderGenerator.Fix;
using Orders.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Services.AddSingleton<FixOrderClient>();
builder.Services.AddHostedService<FixInitiatorService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", (FixOrderClient client) => Results.Ok(new
{
    status = client.IsConnected ? "connected" : "disconnected"
}));

app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    FixOrderClient client,
    CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<OrderSide>(request.Side, true, out var side))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["side"] = ["O lado deve ser Buy ou Sell."] });

    var validationError = OrderRules.Validate(request.Symbol, side, request.Quantity, request.Price);
    if (validationError is not null)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [validationError] });

    var order = new Order(Guid.NewGuid().ToString("N"), request.Symbol, side, request.Quantity, request.Price);

    try
    {
        var response = await client.SendAsync(order, TimeSpan.FromSeconds(10), cancellationToken);
        return Results.Ok(response);
    }
    catch (TimeoutException)
    {
        return Results.Problem("O OrderAccumulator não respondeu dentro do prazo.", statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapFallbackToFile("index.html");
app.Run();
