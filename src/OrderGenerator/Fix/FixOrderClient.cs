using System.Collections.Concurrent;
using OrderGenerator.Contracts;
using Orders.Domain;
using QuickFix;
using QuickFix.Fields;
using Fix44 = QuickFix.FIX44;

namespace OrderGenerator.Fix;

public sealed class FixOrderClient(ILogger<FixOrderClient> logger) : MessageCracker, IApplication
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResponse>> _pending = new();
    private SessionID? _sessionId;

    public bool IsConnected => _sessionId is not null && Session.LookupSession(_sessionId)?.IsLoggedOn == true;

    public async Task<OrderResponse> SendAsync(Order order, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!IsConnected || _sessionId is null)
            throw new InvalidOperationException("A sessão FIX ainda não está conectada.");

        var completion = new TaskCompletionSource<OrderResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(order.ClientOrderId, completion))
            throw new InvalidOperationException("Identificador de ordem duplicado.");

        try
        {
            var message = new Fix44.NewOrderSingle(
                new ClOrdID(order.ClientOrderId),
                new Symbol(order.Symbol),
                new Side(order.Side == OrderSide.Buy ? QuickFix.Fields.Side.BUY : QuickFix.Fields.Side.SELL),
                new TransactTime(DateTime.UtcNow),
                new OrdType(OrdType.LIMIT));
            message.OrderQty = new OrderQty(order.Quantity);
            message.Price = new Price(order.Price);
            message.TimeInForce = new TimeInForce(TimeInForce.DAY);

            if (!Session.SendToTarget(message, _sessionId))
                throw new InvalidOperationException("A ordem não pôde ser enviada pela sessão FIX.");

            return await completion.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            _pending.TryRemove(order.ClientOrderId, out _);
        }
    }

    public void OnMessage(Fix44.ExecutionReport message, SessionID sessionId)
    {
        var clientOrderId = message.ClOrdID.Value;
        if (!_pending.TryGetValue(clientOrderId, out var completion))
        {
            logger.LogWarning("ExecutionReport recebido para ClOrdID desconhecido {ClOrdID}", clientOrderId);
            return;
        }

        var accepted = message.ExecType.Value == ExecType.NEW;
        var text = message.IsSetField(Tags.Text) ? message.Text.Value : accepted ? "Ordem aceita." : "Ordem rejeitada.";
        decimal? exposure = message.IsSetField(9000) ? message.GetDecimal(9000) : null;
        completion.TrySetResult(new(clientOrderId, accepted ? "Accepted" : "Rejected", text, exposure));
    }

    public void OnCreate(SessionID sessionId) => logger.LogInformation("Sessão FIX criada: {Session}", sessionId);

    public void OnLogon(SessionID sessionId)
    {
        _sessionId = sessionId;
        logger.LogInformation("Logon FIX concluído: {Session}", sessionId);
    }

    public void OnLogout(SessionID sessionId)
    {
        _sessionId = null;
        logger.LogWarning("Logout FIX: {Session}", sessionId);
    }

    public void FromAdmin(Message message, SessionID sessionId) { }
    public void ToAdmin(Message message, SessionID sessionId) { }
    public void ToApp(Message message, SessionID sessionId) { }
    public void FromApp(Message message, SessionID sessionId) => Crack(message, sessionId);
}
