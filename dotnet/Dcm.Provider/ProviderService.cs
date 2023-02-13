using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refit;
using Dcm.Provider.ServicesProviders;
using Dcm.Shared.ApiClient;

namespace Dcm.Provider;

public class ProviderService : BackgroundService
{
    private readonly ILogger<ProviderService> _logger;
    private readonly IServicesProvider _servicesProvider;
    private readonly IDcmClient _dcmClient;

    public ProviderService(ILogger<ProviderService> logger, IServicesProvider servicesProvider, IDcmClient dcmClient)
    {
        _logger = logger;
        _servicesProvider = servicesProvider;
        _dcmClient = dcmClient;
    }

    protected override async Task ExecuteAsync (CancellationToken stoppingToken)
    {
        var services = await _servicesProvider.ListServices();
        try
        {
            var sections = await _dcmClient.UpdateDashboard(services);
            _logger.LogDebug($"UpdateDashboard Response Sections");
            foreach (var section in sections)
            {
                _logger.LogDebug(section.ToString());
            }
        }
        catch (ApiException e)
        {
            _logger.LogError($"{nameof(ApiException)}:: {nameof(e.Content)}: {e.Content}, {e}");
        }
    }
}

// public class ProviderService : IHostedService, IDisposable
// {
//     private System.Timers.Timer _timer;
//     private readonly CronExpression _expression;
//     private readonly TimeZoneInfo _timeZoneInfo;

//     protected ProviderService(string cronExpression, TimeZoneInfo timeZoneInfo)
//     {
//         _expression = CronExpression.Parse(cronExpression);
//         _timeZoneInfo = timeZoneInfo;
//     }

//     public virtual async Task StartAsync(CancellationToken cancellationToken)
//     {
//         await ScheduleJob(cancellationToken);
//     }

//     protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
//     {
//         var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
//         if (next.HasValue)
//         {
//             var delay = next.Value - DateTimeOffset.Now;
//             if (delay.TotalMilliseconds <= 0)   // prevent non-positive values from being passed into Timer
//             {
//                 await ScheduleJob(cancellationToken);
//             }
//             _timer = new System.Timers.Timer(delay.TotalMilliseconds);
//             _timer.Elapsed += async (sender, args) =>
//             {
//                 _timer.Dispose();  // reset and dispose timer
//                 _timer = null;

//                 if (!cancellationToken.IsCancellationRequested)
//                 {
//                     await DoWork(cancellationToken);
//                 }

//                 if (!cancellationToken.IsCancellationRequested)
//                 {
//                     await ScheduleJob(cancellationToken);    // reschedule next
//                 }
//             };
//             _timer.Start();
//         }
//         await Task.CompletedTask;
//     }

//     public virtual async Task DoWork(CancellationToken cancellationToken)
//     {
//         await Task.Delay(5000, cancellationToken);  // do the work
//     }

//     public virtual async Task StopAsync(CancellationToken cancellationToken)
//     {
//         _timer?.Stop();
//         await Task.CompletedTask;
//     }

//     public virtual void Dispose()
//     {
//         _timer?.Dispose();
//     }
// }