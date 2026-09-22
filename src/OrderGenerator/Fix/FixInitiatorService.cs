using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Fix;

public sealed class FixInitiatorService(FixOrderClient application, IWebHostEnvironment environment) : IHostedService
{
    private IInitiator? _initiator;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(environment.ContentRootPath, "store", "generator"));
        Directory.CreateDirectory(Path.Combine(environment.ContentRootPath, "logs", "generator"));
        var settings = new SessionSettings(Path.Combine(environment.ContentRootPath, "Config", "initiator.cfg"));
        var dictionaryPath = Path.Combine(AppContext.BaseDirectory, "Config", "FIX44.xml");
        foreach (var sessionId in settings.GetSessions())
        {
            var sessionSettings = settings.Get(sessionId);
            sessionSettings.SetString("DataDictionary", dictionaryPath);
        }
        _initiator = new SocketInitiator(
            application,
            new FileStoreFactory(settings),
            settings,
            new FileLogFactory(settings));
        _initiator.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _initiator?.Stop();
        return Task.CompletedTask;
    }
}
