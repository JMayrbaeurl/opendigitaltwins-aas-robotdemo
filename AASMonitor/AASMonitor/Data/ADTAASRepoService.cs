using Azure.DigitalTwins.Core;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AASMonitor.Data
{
    public class ADTAASRepoService
    {
        private readonly IConfiguration _configuration;

        private readonly DigitalTwinsClient _digitalTwinsClient;

        private readonly double _intervalInSeconds;

        private ConcurrentDictionary<string, string> lastValues = new ConcurrentDictionary<string, string>();

        public static List<string> tagNames = new List<string>() {
            "ActualAxisPosition_A1", "ActualAxisPosition_A2", "ActualAxisPosition_A3",
            "ActualAxisPosition_A4", "ActualAxisPosition_A5", "ActualAxisPosition_A6",
            "ActualKarthPositon_X", "ActualKarthPositon_Y", "ActualKarthPositon_Z",
            "ActualKarthPositon_A", "ActualKarthPositon_B", "ActualKarthPositon_C"};

        public ADTAASRepoService(DigitalTwinsClient digitalTwinsClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _digitalTwinsClient = digitalTwinsClient;
            _intervalInSeconds = configuration.GetValue<double>("PollFrequencyInSeconds");
        }

        public async Task UpdateLatestValues()
        {
            string queryString = $"SELECT * FROM digitaltwins where $dtId in " +
                $"['ActualAxisPosition_A1','ActualAxisPosition_A2','ActualAxisPosition_A3'," +
                $"'ActualAxisPosition_A4','ActualAxisPosition_A5','ActualAxisPosition_A6'," +
                $"'ActualKarthPositon_X','ActualKarthPositon_Y','ActualKarthPositon_Z'," +
                $"'ActualKarthPositon_A','ActualKarthPositon_B','ActualKarthPositon_C']";

            var queryResult = this._digitalTwinsClient.QueryAsync<BasicDigitalTwin>(queryString);
            if (queryResult != null)
            {
                await foreach (var item in queryResult)
                {
                    if (item.Contents["value"] != null)
                    {
                        string newValue = item.Contents["value"].ToString();
                        this.lastValues.AddOrUpdate(item.Id, newValue, (key, oldvalue) => newValue);
                    }
                }
            }
        }

        public string LastValueFor(string propName)
        {
            if (!String.IsNullOrEmpty(propName) && this.lastValues != null && this.lastValues.ContainsKey(propName))
                return this.lastValues[propName];
            else
                return "";
        }

        public double PullFrequencyInSeconds()
        {
            return this._intervalInSeconds;
        }

        public string ADTEndpoint()
        {
            return _configuration["ADTEndpoint"];
        }
    }

    public class TimedHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedHostedService> _logger;
        private Timer _timer;

        private ADTAASRepoService adtService;

        public TimedHostedService(ILogger<TimedHostedService> logger, ADTAASRepoService service)
        {
            _logger = logger;
            adtService = service;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(this.adtService.PullFrequencyInSeconds()));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);

            if (adtService != null)
            {
                adtService.UpdateLatestValues();
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
