using System;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels
{
    public enum ScaleRangeType
    {
        ScheduledTimesOfJob = 1,
        HistoricalRuns = 2,
        OEErunSpeed = 3,
        CostingSpeedCalculation = 4,
    }

    public enum PointerSource
    {
        CurrentSpeed,
        AverageSpeed,
        FeedsHourInclusiveStops,
        AverageSpeedExclStops,
    }

    /// <summary>Snapshot of all values the speedometer needs (LineUp + SP pS_Dashboard_SpeedometerValues).</summary>
    public sealed class SpeedometerData
    {
        public string MachineId { get; set; }
        public string Description { get; set; }
        public string JobHandle { get; set; }
        public string CustomText { get; set; }
        public bool IsConverter { get; set; } = true;

        public double CurrentSpeed { get; set; }
        public double AverageSpeed { get; set; }
        public double FeedsHourInclusiveStops { get; set; }
        public double AverageSpeedExclStops { get; set; }
        public double AvgScheduledShiftSpeed { get; set; }

        public double ExpectedSpeed { get; set; }
        public double HighestSpeedAcceptedPriority { get; set; }
        public double MaximumSpeed { get; set; }

        public int Jobs { get; set; }
        public double? AvgMark { get; set; }
        public double? AvgPlusMark { get; set; }
        public double? AvgMinusMark { get; set; }
        public double? MaxMark { get; set; }
        public double? FeedsMark { get; set; }
        public double? OeeMark { get; set; }

        /// <summary>True when the LineUp MachineStatus is not "Started".</summary>
        public bool MachineDown { get; set; }
        public string DownCode { get; set; }
        public string DownReason { get; set; }
        public TimeSpan Downtime { get; set; }
    }

    /// <summary>
    /// Connection point to the real data (LineUpClient + SP pS_Dashboard_SpeedometerValues).
    /// Raise <see cref="DataChanged"/> whenever new values are available (any thread).
    /// </summary>
    public interface ISpeedometerDataSource : IDisposable
    {
        event EventHandler<SpeedometerData> DataChanged;

        void Start();

        void ApplySettings(MachineSpeedometerSettings settings);
    }
}
