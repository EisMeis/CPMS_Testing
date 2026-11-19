using System;
using System.Windows.Threading;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    /// <summary>
    /// Demo source with random values. Replace it by your own <see cref="ISpeedometerDataSource"/>
    /// that wraps LineUpClient (current/expected/highest/maximum speed, machine status)
    /// and the SP pS_Dashboard_SpeedometerValues (marks), see Readme in the chat.
    /// </summary>
    public sealed class SimulatedDataSource : ISpeedometerDataSource
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private readonly Random rnd = new Random();
        private DateTime downSince;
        private bool down;

        public event EventHandler<SpeedometerData> DataChanged;

        public SimulatedDataSource() => timer.Tick += (s, e) => Publish();

        public void Start() { Publish(); timer.Start(); }
        public void ApplySettings(MachineSpeedometerSettings settings) =>
            timer.Interval = TimeSpan.FromSeconds(settings.RefreshIntervalSeconds);
        public void Dispose() => timer.Stop();

        private void Publish()
        {
            if (rnd.Next(8) == 0)
            {
                down = !down;
                downSince = DateTime.Now;
            }
            double cur = down ? 0 : 6000 + rnd.NextDouble() * 5000;
            DataChanged?.Invoke(this, new SpeedometerData
            {
                MachineId = "CONV-01",
                Description = "Converter Line 1",
                JobHandle = "JOB-4711",
                CustomText = "Demo",
                IsConverter = true,
                CurrentSpeed = cur,
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
                MachineDown = down,
                DownCode = "D12",
                DownReason = "Rollenwechsel",
                Downtime = down ? DateTime.Now - downSince : TimeSpan.Zero
            });
        }
    }
}
