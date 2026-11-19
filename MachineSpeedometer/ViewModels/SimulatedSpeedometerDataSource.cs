using System;
using System.Windows.Threading;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels
{
    /// <summary>
    /// Test data source with random values. Replace it with your own <see cref="ISpeedometerDataSource"/>
    /// that wraps LineUpClient (current/expected/highest/maximum speed, machine status)
    /// and the stored procedure pS_Dashboard_SpeedometerValues (marks).
    /// </summary>
    public sealed class SimulatedSpeedometerDataSource : ISpeedometerDataSource
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private readonly Random random = new Random();
        private DateTime downSince;
        private bool down;

        public SimulatedSpeedometerDataSource()
        {
            this.timer.Tick += (s, e) => this.Publish();
        }

        public event EventHandler<SpeedometerData> DataChanged;

        public void Start()
        {
            this.Publish();
            this.timer.Start();
        }

        public void ApplySettings(MachineSpeedometerSettings settings)
        {
            this.timer.Interval = TimeSpan.FromSeconds(settings.RefreshIntervalSeconds);
        }

        public void Dispose()
        {
            this.timer.Stop();
        }

        private void Publish()
        {
            if (this.random.Next(8) == 0)
            {
                this.down = !this.down;
                this.downSince = DateTime.Now;
            }

            this.DataChanged?.Invoke(
                this,
                new SpeedometerData
                {
                    MachineId = "CONV-01",
                    Description = "Converter Line 1",
                    JobHandle = "JOB-4711",
                    CustomText = "Demo",
                    IsConverter = true,
                    CurrentSpeed = this.down ? 0 : 6000 + (this.random.NextDouble() * 5000),
                    AverageSpeed = 8000,
                    FeedsHourInclusiveStops = 7400,
                    AverageSpeedExclStops = 8300,
                    AvgScheduledShiftSpeed = 7900,
                    ExpectedSpeed = 7000,
                    HighestSpeedAcceptedPriority = 9500,
                    MaximumSpeed = 12000,
                    Jobs = 3,
                    AvgMark = 8000,
                    AvgPlusMark = 9000,
                    AvgMinusMark = 7000,
                    MaxMark = 11000,
                    FeedsMark = 7400,
                    OeeMark = 8500,
                    MachineDown = this.down,
                    DownCode = "D12",
                    DownReason = "Rollenwechsel",
                    Downtime = this.down ? DateTime.Now - this.downSince : TimeSpan.Zero,
                });
        }
    }
}
