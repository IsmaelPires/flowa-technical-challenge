using Microsoft.Extensions.Logging;
using QuickFix;
using Fix44 = QuickFix.FIX44;

namespace OrderAccumulator;

public sealed class AccumulatorApplication(FixOrderProcessor processor, ILogger<AccumulatorApplication> logger)
    : MessageCracker, IApplication
{
    public void OnMessage(Fix44.NewOrderSingle message, SessionID sessionId)
    {
        var report = processor.Process(message);
        Session.SendToTarget(report, sessionId);

        logger.LogInformation(
            "Ordem {ClOrdID} {Result}. {Symbol} {Side} {Quantity} @ {Price}; exposição: {Exposure}",
            message.ClOrdID.Value,
            report.ExecType.Value == QuickFix.Fields.ExecType.NEW ? "aceita" : "rejeitada",
            message.Symbol.Value,
            message.Side.Value,
            message.OrderQty.Value,
            message.Price.Value,
            report.GetDecimal(9000));
    }

    public void OnCreate(SessionID sessionId) => logger.LogInformation("Sessão FIX criada: {Session}", sessionId);
    public void OnLogon(SessionID sessionId) => logger.LogInformation("Cliente conectado: {Session}", sessionId);
    public void OnLogout(SessionID sessionId) => logger.LogInformation("Cliente desconectado: {Session}", sessionId);
    public void FromAdmin(Message message, SessionID sessionId) { }
    public void ToAdmin(Message message, SessionID sessionId) { }
    public void ToApp(Message message, SessionID sessionId) { }
    public void FromApp(Message message, SessionID sessionId) => Crack(message, sessionId);
}
