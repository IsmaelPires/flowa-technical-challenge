using Microsoft.Extensions.Logging;
using OrderAccumulator;
using Orders.Domain;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.SetMinimumLevel(LogLevel.Information).AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    }));

var ledger = new ExposureLedger();
var processor = new FixOrderProcessor(ledger);
var application = new AccumulatorApplication(processor, loggerFactory.CreateLogger<AccumulatorApplication>());
var settingsPath = Path.Combine(AppContext.BaseDirectory, "Config", "acceptor.cfg");
var settings = new SessionSettings(settingsPath);
var dictionaryPath = Path.Combine(AppContext.BaseDirectory, "Config", "FIX44.xml");
foreach (var sessionId in settings.GetSessions())
{
    var sessionSettings = settings.Get(sessionId);
    sessionSettings.SetString("DataDictionary", dictionaryPath);
}

Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "store", "accumulator"));
Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs", "accumulator"));

using var acceptor = new ThreadedSocketAcceptor(
    application,
    new FileStoreFactory(settings),
    settings,
    new FileLogFactory(settings));

acceptor.Start();
Console.WriteLine("OrderAccumulator ativo em FIX 4.4 / porta 5001. Pressione Ctrl+C para encerrar.");

var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopped.TrySetResult();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => stopped.TrySetResult();
await stopped.Task;
acceptor.Stop();
