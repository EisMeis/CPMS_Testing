using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels
{
    /// <summary>One bookmark (AVG, AVG+, ...) drawn as a thin range bar on the scale.</summary>
    public sealed class SpeedMark : SpeedometerBindableBase
    {
        private double start;
        private double end;
        private bool isVisible;
        private string text;

        public SpeedMark(string label, Color color)
        {
            this.Label = label;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            this.Brush = brush;
        }

        public string Label { get; }

        public Brush Brush { get; }

        public double Start { get => this.start; private set => this.Set(ref this.start, value); }

        public double End { get => this.end; private set => this.Set(ref this.end, value); }

        public bool IsVisible { get => this.isVisible; private set => this.Set(ref this.isVisible, value); }

        public string Text { get => this.text; private set => this.Set(ref this.text, value); }

        /// <summary>Hidden marks collapse to a zero-length bar.</summary>
        public void Update(bool show, double value, double scaleMax)
        {
            double width = scaleMax * 0.008;
            double v = Math.Max(0, Math.Min(value, scaleMax));
            this.IsVisible = show;
            this.Start = show ? Math.Max(0, v - width) : 0;
            this.End = show ? Math.Min(scaleMax, v + width) : 0;
            this.Text = show ? this.Label + " " + Math.Round(value).ToString("0") : string.Empty;
        }
    }

    /// <summary>Value that moves smoothly (cubic smoothstep) from its old to a new target.</summary>
    internal sealed class AnimatedValue
    {
        private double start;
        private double target;
        private double amount = 1;

        public double Current { get; private set; }

        public bool HasValue { get; private set; }

        public void SetTarget(double value)
        {
            if (value < 0)
                return; // negative speeds are ignored

            if (!this.HasValue)
            {
                this.HasValue = true;
                this.Current = this.start = this.target = value;
                this.amount = 1;
                return;
            }

            if (value == this.target)
                return;

            this.start = this.Current;
            this.target = value;
            this.amount = 0.1;
        }

        public bool Step()
        {
            if (this.amount >= 1)
                return false;

            this.amount = Math.Min(1, this.amount + (this.amount / 10));
            double t = this.amount;
            this.Current = this.start + ((this.target - this.start) * (t * t * (3 - (2 * t))));
            if (this.amount >= 1)
                this.Current = this.target;

            return true;
        }
    }

    public sealed class MachineSpeedometerViewModel : SpeedometerBindableBase, IDisposable
    {
        public const double ScaleStartAngle = -225;
        public const double ScaleEndAngle = 45;

        private readonly ISpeedometerDataSource source;
        private readonly DispatcherTimer animationTimer;
        private readonly DispatcherTimer flashingTimer;

        private readonly AnimatedValue pointer = new AnimatedValue();
        private readonly AnimatedValue avg = new AnimatedValue();
        private readonly AnimatedValue avgPlus = new AnimatedValue();
        private readonly AnimatedValue avgMinus = new AnimatedValue();
        private readonly AnimatedValue max = new AnimatedValue();
        private readonly AnimatedValue feeds = new AnimatedValue();
        private readonly AnimatedValue oee = new AnimatedValue();
        private readonly AnimatedValue crew = new AnimatedValue();

        private SpeedometerData data = new SpeedometerData();
        private bool flashOn;
        private bool disposed;

        public MachineSpeedometerViewModel(ISpeedometerDataSource source, string settingsKey = "default")
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));

            this.SettingsPath = MachineSpeedometerSettings.DefaultPath(settingsKey);
            this.Settings = new MachineSpeedometerSettings();
            this.Settings.Load(this.SettingsPath);
            this.Settings.PropertyChanged += (s, e) => this.Refresh();

            this.Marks = new List<SpeedMark>
            {
                new SpeedMark("AVG", Colors.Cyan),
                new SpeedMark("AVG+", Colors.Orange),
                new SpeedMark("AVG-", Colors.Gold),
                new SpeedMark("MAX", Colors.Magenta),
                new SpeedMark("Feeds", Colors.DeepSkyBlue),
                new SpeedMark("OEE", Colors.Violet),
                new SpeedMark("Crew", Colors.White),
            };

            this.animationTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
            this.animationTimer.Tick += (s, e) => this.AnimationTick();

            this.flashingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            this.flashingTimer.Tick += (s, e) =>
            {
                this.flashOn = !this.flashOn;
                if (this.data.MachineDown)
                    this.Refresh();
            };

            this.source.DataChanged += this.HandleDataChanged;
            this.source.ApplySettings(this.Settings);
            this.source.Start();

            this.animationTimer.Start();
            this.flashingTimer.Start();
            this.Refresh();
        }

        #region ---------- Settings ----------

        public MachineSpeedometerSettings Settings { get; }

        public string SettingsPath { get; }

        public void ApplySettings(MachineSpeedometerSettings edited)
        {
            this.Settings.CopyFrom(edited);
            this.source.ApplySettings(this.Settings);
            this.Settings.Save(this.SettingsPath);
        }

        #endregion

        #region ---------- Scale ----------

        public double StartAngle => ScaleStartAngle;

        public double EndAngle => ScaleEndAngle;

        /// <summary>Scale maximum; rounded up by 15% when expected/highest exceed the maximum speed.</summary>
        public double ScaleMax
        {
            get
            {
                double m = Math.Max(1, this.data.MaximumSpeed);
                double top = Math.Max(this.data.ExpectedSpeed, this.data.HighestSpeedAcceptedPriority);
                return top > m ? Math.Ceiling(top * 1.15) : m;
            }
        }

        public double NeedleValue => this.Clamp(this.pointer.Current);

        public bool IsDown => this.data.MachineDown;

        private double ExpectedEnd => this.IsDown ? this.ScaleMax : this.Clamp(this.data.ExpectedSpeed);

        private double HighestEnd => this.IsDown ? this.ScaleMax : Math.Max(this.ExpectedEnd, this.Clamp(this.data.HighestSpeedAcceptedPriority));

        public double Zone1End => this.ExpectedEnd;

        public double Zone2End => this.HighestEnd;

        public Brush Zone1Brush => this.IsDown ? this.FlashingBrush : this.Frozen(this.Settings.ExpectedColor);

        public Brush Zone2Brush => this.Frozen(this.Settings.HighestColor);

        public Brush Zone3Brush => this.Frozen(this.Settings.MaximumColor);

        public IList<SpeedMark> Marks { get; }

        #endregion

        #region ---------- Texts and colors ----------

        public Brush BackgroundBrush => this.Settings.TransparentMode ? Brushes.Transparent : this.Frozen(this.Settings.BackgroundColor);

        public Brush ForegroundBrush => this.Frozen(this.Settings.ForegroundColor);

        public Brush ValueBrush => this.IsDown ? this.FlashingBrush : this.Frozen(this.SpeedColor());

        public string ValueText => this.IsDown ? (this.data.DownCode ?? string.Empty) : Math.Round(this.data.CurrentSpeed).ToString("0");

        public string UnitText => this.data.IsConverter ? "feeds/h" : "m/min";

        public bool ShowUnit => !this.IsDown;

        public string DowntimeText => this.data.Downtime.ToString(@"hh\:mm\:ss");

        public bool ShowDowntime => this.IsDown;

        public string FeedsText => Math.Round(this.data.FeedsHourInclusiveStops).ToString("0");

        public string AvgShiftText => ((int)Math.Round(this.data.AvgScheduledShiftSpeed)).ToString("00000");

        public string MachineIdText => this.data.MachineId ?? string.Empty;

        public string DescriptionText => this.data.Description ?? string.Empty;

        public string JobText => this.data.JobHandle ?? string.Empty;

        public string CustomText => this.IsDown && !string.IsNullOrEmpty(this.data.DownReason) ? this.data.DownReason : (this.data.CustomText ?? string.Empty);

        public bool ShowSpeedLabel => this.Settings.ShowSpeedLabel;

        public bool ShowFeedsLabel => this.Settings.ShowFeedsLabel;

        public bool ShowAvgShiftLabel => this.Settings.ShowAvgShiftLabel;

        public bool ShowMachineId => this.Settings.ShowMachineId;

        public bool ShowDescription => this.Settings.ShowDescription;

        public bool ShowJobHandle => this.Settings.ShowJobHandle;

        public bool ShowCustomText => this.Settings.ShowCustomText;

        public IEnumerable<KeyValuePair<string, string>> InfoItems => new[]
        {
            new KeyValuePair<string, string>("Maschine", this.data.MachineId),
            new KeyValuePair<string, string>("Beschreibung", this.data.Description),
            new KeyValuePair<string, string>("Job", this.data.JobHandle),
            new KeyValuePair<string, string>("Aktuelle Geschwindigkeit", this.data.CurrentSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Durchschnitt", this.data.AverageSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Erwartet", this.data.ExpectedSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Höchste Priorität", this.data.HighestSpeedAcceptedPriority.ToString("0.#")),
            new KeyValuePair<string, string>("Maximum", this.data.MaximumSpeed.ToString("0.#")),
            new KeyValuePair<string, string>("Skalenmaximum", this.ScaleMax.ToString("0.#")),
            new KeyValuePair<string, string>("Status", this.IsDown ? "Stillstand" : "Läuft"),
        };

        private Brush FlashingBrush
        {
            get
            {
                var c = this.Settings.FlashingColor;
                return this.Frozen(this.flashOn ? c : Color.FromRgb((byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2)));
            }
        }

        private Color SpeedColor()
        {
            double v = this.data.CurrentSpeed;
            if (v <= 0)
                return this.Settings.ForegroundColor;
            if (v <= this.data.ExpectedSpeed)
                return this.Settings.ExpectedColor;
            if (v <= this.data.HighestSpeedAcceptedPriority)
                return this.Settings.HighestColor;
            return this.Settings.MaximumColor;
        }

        #endregion

        #region ---------- Data and animation ----------

        private void HandleDataChanged(object sender, SpeedometerData e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => this.HandleDataChanged(sender, e)));
                return;
            }

            if (this.disposed || e == null)
                return;

            this.data = e;
            this.Refresh();
        }

        private void SyncTargets()
        {
            double p;
            switch (this.Settings.PointerSource)
            {
                case PointerSource.AverageSpeed:
                    p = this.data.AverageSpeed;
                    break;
                case PointerSource.FeedsHourInclusiveStops:
                    p = this.data.FeedsHourInclusiveStops;
                    break;
                case PointerSource.AverageSpeedExclStops:
                    p = this.data.AverageSpeedExclStops;
                    break;
                default:
                    p = this.data.CurrentSpeed;
                    break;
            }

            this.pointer.SetTarget(p);
            if (this.data.AvgMark.HasValue)
                this.avg.SetTarget(this.data.AvgMark.Value);
            if (this.data.AvgPlusMark.HasValue)
                this.avgPlus.SetTarget(this.data.AvgPlusMark.Value);
            if (this.data.AvgMinusMark.HasValue)
                this.avgMinus.SetTarget(this.data.AvgMinusMark.Value);
            if (this.data.MaxMark.HasValue)
                this.max.SetTarget(this.data.MaxMark.Value);
            if (this.data.FeedsMark.HasValue)
                this.feeds.SetTarget(this.data.FeedsMark.Value);
            if (this.data.OeeMark.HasValue)
                this.oee.SetTarget(this.data.OeeMark.Value);

            // The CrewId mark follows the highest accepted speed.
            this.crew.SetTarget(this.data.HighestSpeedAcceptedPriority);
        }

        private void AnimationTick()
        {
            bool changed = false;
            foreach (var a in new[] { this.pointer, this.avg, this.avgPlus, this.avgMinus, this.max, this.feeds, this.oee, this.crew })
                changed |= a.Step();

            if (changed)
                this.UpdateView();
        }

        private void Refresh()
        {
            this.SyncTargets();
            this.UpdateView();
        }

        private void UpdateView()
        {
            double m = this.ScaleMax;
            var s = this.Settings;
            bool jobs = this.data.Jobs > 0;

            this.Marks[0].Update(s.ShowAvg && this.data.AvgMark.HasValue, this.avg.Current, m);
            this.Marks[1].Update(s.ShowAvgPlus && jobs && this.data.AvgPlusMark.HasValue, this.avgPlus.Current, m);
            this.Marks[2].Update(s.ShowAvgMinus && jobs && this.data.AvgMinusMark.HasValue, this.avgMinus.Current, m);
            this.Marks[3].Update(s.ShowMax && this.data.MaxMark.HasValue, this.max.Current, m);
            this.Marks[4].Update(s.ShowFeeds && this.data.FeedsMark.HasValue, this.feeds.Current, m);
            this.Marks[5].Update(s.ShowOee && this.data.OeeMark.HasValue, this.oee.Current, m);
            this.Marks[6].Update(s.ShowCrewId && this.data.HighestSpeedAcceptedPriority > 0, this.crew.Current, m);

            this.RaisePropertyChanged(null);
        }

        private double Clamp(double value) => Math.Max(0, Math.Min(value, this.ScaleMax));

        private Brush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion

        public void Dispose()
        {
            if (this.disposed)
                return;

            this.disposed = true;
            this.animationTimer.Stop();
            this.flashingTimer.Stop();
            this.source.DataChanged -= this.HandleDataChanged;
            this.source.Dispose();
        }
    }
}
