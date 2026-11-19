using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Threading;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    /// <summary>Value that moves smoothly (cubic smoothstep) from its old to a new target.</summary>
    internal sealed class AnimatedValue
    {
        private double start, target, amount = 1;
        public double Current { get; private set; }
        public bool HasValue { get; private set; }

        public void SetTarget(double value)
        {
            if (value < 0) return; // negative speeds are ignored
            if (!HasValue)
            {
                HasValue = true;
                Current = start = target = value;
                amount = 1;
                return;
            }
            if (value == target) return;
            start = Current;
            target = value;
            amount = 0.1;
        }

        public bool Step()
        {
            if (amount >= 1) return false;
            amount = Math.Min(1, amount + amount / 10);
            double t = amount;
            Current = start + (target - start) * (t * t * (3 - 2 * t));
            if (amount >= 1) Current = target;
            return true;
        }
    }

    public sealed class MachineSpeedometerViewModel : ViewModelBase, IDisposable
    {
        public const double StartAngle = 135, RangeAngle = 270;

        private readonly ISpeedometerDataSource source;
        private readonly DispatcherTimer animationTimer, flashingTimer;
        private readonly AnimatedValue pointer = new AnimatedValue(), avg = new AnimatedValue(),
            max = new AnimatedValue(), feeds = new AnimatedValue(), oee = new AnimatedValue(),
            avgPlus = new AnimatedValue(), avgMinus = new AnimatedValue(), crew = new AnimatedValue();
        private SpeedometerData data = new SpeedometerData();
        private bool flashOn, disposed;

        /// <summary>Raised whenever the picture must be redrawn.</summary>
        public event EventHandler RenderRequested;

        public MachineSpeedometerViewModel(ISpeedometerDataSource source, string settingsKey = "default")
        {
            SettingsPath = MachineSpeedometerSettings.DefaultPath(settingsKey);
            Settings = new MachineSpeedometerSettings();
            Settings.Load(SettingsPath);
            Settings.PropertyChanged += (s, e) => { this.SyncTargets(); Request(); };

            animationTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
            animationTimer.Tick += (s, e) => AnimationTick();
            flashingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            flashingTimer.Tick += (s, e) => { flashOn = !flashOn; if (data.MachineDown) Request(); };

            this.source = source;
            source.DataChanged += HandleDataChanged;
            source.ApplySettings(Settings);
            source.Start();
            animationTimer.Start();
            flashingTimer.Start();
        }

        public MachineSpeedometerSettings Settings { get; }
        public string SettingsPath { get; }
        public SpeedometerData Data => data;

        public double PointerValue => pointer.Current;
        public double AvgValue => avg.Current;
        public double AvgPlusValue => avgPlus.Current;
        public double AvgMinusValue => avgMinus.Current;
        public double MaxValue => max.Current;
        public double FeedsValue => feeds.Current;
        public double OeeValue => oee.Current;
        public double CrewIdValue => crew.Current;

        public bool IsDown => data.MachineDown;
        public bool FlashOn => flashOn;

        /// <summary>Scale maximum; rounded up by 15% when expected/highest exceed the maximum speed.</summary>
        public double ScaleMax
        {
            get
            {
                double m = Math.Max(1, data.MaximumSpeed);
                double top = Math.Max(data.ExpectedSpeed, data.HighestSpeedAcceptedPriority);
                if (top > m) m = Math.Ceiling(top * 1.15);
                return m;
            }
        }

        public string UnitText => data.IsConverter ? "feeds/h" : "m/min";

        public string SpeedText => IsDown ? (data.DownCode ?? string.Empty) : Math.Round(CurrentShown).ToString("0");
        public double CurrentShown => data.CurrentSpeed;
        public string FeedsText => Math.Round(data.FeedsHourInclusiveStops).ToString("0");
        public string AvgShiftText => ((int)Math.Round(data.AvgScheduledShiftSpeed)).ToString("00000");
        public string DowntimeText => data.Downtime.ToString(@"hh\:mm\:ss");
        public string BottomText => IsDown && !string.IsNullOrEmpty(data.DownReason) ? data.DownReason : data.CustomText;

        /// <summary>Colour of the numeric label depending on the speed zone.</summary>
        public Color SpeedColor
        {
            get
            {
                double v = data.CurrentSpeed;
                if (v <= 0) return Settings.ForegroundColor;
                if (v <= data.ExpectedSpeed) return Settings.ExpectedColor;
                if (v <= data.HighestSpeedAcceptedPriority) return Settings.HighestColor;
                return Settings.MaximumColor;
            }
        }

        /// <summary>Flashing colour used instead of the zones in downtime mode.</summary>
        public Color FlashingScaleColor
        {
            get
            {
                var c = Settings.FlashingColor;
                if (flashOn) return c;
                return Color.FromRgb((byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2));
            }
        }

        public IEnumerable<KeyValuePair<string, string>> InfoItems => new[]
        {
            new KeyValuePair<string, string>("Maschine", data.MachineId),
            new KeyValuePair<string, string>("Beschreibung", data.Description),
            new KeyValuePair<string, string>("Job", data.JobHandle),
            new KeyValuePair<string, string>("Aktuelle Geschwindigkeit", data.CurrentSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Durchschnitt", data.AverageSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Erwartet", data.ExpectedSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Höchste Priorität", data.HighestSpeedAcceptedPriority.ToString("0.#")),
            new KeyValuePair<string, string>("Maximum", data.MaximumSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Skalenmaximum", ScaleMax.ToString("0.#")),
            new KeyValuePair<string, string>("Status", data.MachineDown ? "Stillstand" : "Läuft"),
        };

        public double ValueToAngle(double value)
        {
            double v = Math.Max(0, Math.Min(value, ScaleMax));
            return StartAngle + v / ScaleMax * RangeAngle;
        }

        public void SaveSettings() => Settings.Save(SettingsPath);

        public void ApplySettings(MachineSpeedometerSettings edited)
        {
            Settings.CopyFrom(edited);
            source.ApplySettings(Settings);
            SaveSettings();
        }

        private void HandleDataChanged(object sender, SpeedometerData e)
        {
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d != null && !d.CheckAccess())
            {
                d.BeginInvoke(new Action(() => HandleDataChanged(sender, e)));
                return;
            }
            if (disposed || e == null) return;
            data = e;
            SyncTargets();
            RaisePropertyChanged(string.Empty);
            Request();
        }

        private void SyncTargets()
        {
            var s = Settings;
            double p;
            switch (s.PointerSource)
            {
                case PointerSource.AverageSpeed: p = data.AverageSpeed; break;
                case PointerSource.FeedsHourInclusiveStops: p = data.FeedsHourInclusiveStops; break;
                case PointerSource.AverageSpeedExclStops: p = data.AverageSpeedExclStops; break;
                default: p = data.CurrentSpeed; break;
            }
            pointer.SetTarget(p);
            if (data.AvgMark.HasValue) avg.SetTarget(data.AvgMark.Value);
            if (data.MaxMark.HasValue) max.SetTarget(data.MaxMark.Value);
            if (data.FeedsMark.HasValue) feeds.SetTarget(data.FeedsMark.Value);
            if (data.OeeMark.HasValue) oee.SetTarget(data.OeeMark.Value);
            if (data.AvgPlusMark.HasValue) avgPlus.SetTarget(data.AvgPlusMark.Value);
            if (data.AvgMinusMark.HasValue) avgMinus.SetTarget(data.AvgMinusMark.Value);
            crew.SetTarget(data.HighestSpeedAcceptedPriority); // CrewId mark follows the highest speed
        }

        private void AnimationTick()
        {
            bool changed = false;
            foreach (var a in new[] { pointer, avg, avgPlus, avgMinus, max, feeds, oee, crew })
                changed |= a.Step();
            if (changed) Request();
        }

        private void Request() => RenderRequested?.Invoke(this, EventArgs.Empty);

        public bool ShowAvgMark => Settings.ShowAvg && data.AvgMark.HasValue;
        public bool ShowAvgPlusMark => Settings.ShowAvgPlus && data.Jobs > 0 && data.AvgPlusMark.HasValue;
        public bool ShowAvgMinusMark => Settings.ShowAvgMinus && data.Jobs > 0 && data.AvgMinusMark.HasValue;
        public bool ShowMaxMark => Settings.ShowMax && data.MaxMark.HasValue;
        public bool ShowFeedsMark => Settings.ShowFeeds && data.FeedsMark.HasValue;
        public bool ShowOeeMark => Settings.ShowOee && data.OeeMark.HasValue;
        public bool ShowCrewMark => Settings.ShowCrewId && data.HighestSpeedAcceptedPriority > 0;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            animationTimer.Stop();
            flashingTimer.Stop();
            source.DataChanged -= HandleDataChanged;
            source.Dispose();
        }
    }
}
