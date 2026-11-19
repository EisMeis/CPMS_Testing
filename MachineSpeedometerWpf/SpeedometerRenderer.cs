using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    /// <summary>Draws the whole speedometer (zones, scale, marks, needle, labels) directly.</summary>
    public sealed class SpeedometerRenderer : FrameworkElement
    {
        private MachineSpeedometerViewModel vm;

        public MachineSpeedometerViewModel ViewModel
        {
            get => vm;
            set
            {
                if (vm != null) vm.RenderRequested -= HandleRender;
                vm = value;
                if (vm != null) vm.RenderRequested += HandleRender;
                InvalidateVisual();
            }
        }

        private void HandleRender(object sender, EventArgs e) => InvalidateVisual();

        private static Brush B(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

        private FormattedText Text(string s, double size, Color c, FontWeight? w = null) =>
            new FormattedText(s ?? string.Empty, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, w ?? FontWeights.Normal, FontStretches.Normal),
                Math.Max(4, size), B(c), VisualTreeHelper.GetDpi(this).PixelsPerDip);

        private static Point Pt(Point c, double r, double deg)
        {
            double a = deg * Math.PI / 180;
            return new Point(c.X + r * Math.Cos(a), c.Y + r * Math.Sin(a));
        }

        private static Geometry Arc(Point c, double rOuter, double rInner, double a0, double a1)
        {
            var g = new StreamGeometry();
            double sweep = a1 - a0;
            bool large = sweep > 180;
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(Pt(c, rOuter, a0), true, true);
                ctx.ArcTo(Pt(c, rOuter, a1), new Size(rOuter, rOuter), 0, large, SweepDirection.Clockwise, true, false);
                ctx.LineTo(Pt(c, rInner, a1), true, false);
                ctx.ArcTo(Pt(c, rInner, a0), new Size(rInner, rInner), 0, large, SweepDirection.Counterclockwise, true, false);
            }
            g.Freeze();
            return g;
        }

        private void Centered(DrawingContext dc, FormattedText t, Point center)
            => dc.DrawText(t, new Point(center.X - t.Width / 2, center.Y - t.Height / 2));

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (vm == null || w < 20 || h < 20) return;
            var s = vm.Settings;

            double size = Math.Min(w, h);
            var c = new Point(w / 2, h / 2);
            double R = size / 2 - 3;

            if (!s.TransparentMode)
                dc.DrawRectangle(B(s.BackgroundColor), null, new Rect(0, 0, w, h));

            dc.DrawEllipse(null, new Pen(B(s.TransparentMode ? Colors.Transparent : s.BorderColor), Math.Max(1, R * 0.03)), c, R, R);

            // colour zones
            double bandOut = R * 0.93, bandIn = R * 0.81;
            double max = vm.ScaleMax;
            var d = vm.Data;
            double a0 = MachineSpeedometerViewModel.StartAngle;
            double aEnd = a0 + MachineSpeedometerViewModel.RangeAngle - 0.01;

            if (vm.IsDown)
            {
                dc.DrawGeometry(B(vm.FlashingScaleColor), null, Arc(c, bandOut, bandIn, a0, aEnd));
            }
            else
            {
                double e = Math.Min(d.ExpectedSpeed, max), hi = Math.Max(e, Math.Min(d.HighestSpeedAcceptedPriority, max));
                void Zone(double from, double to, Color col)
                {
                    if (to - from <= 0) return;
                    dc.DrawGeometry(B(col), null, Arc(c, bandOut, bandIn, vm.ValueToAngle(from), Math.Min(aEnd, vm.ValueToAngle(to))));
                }
                Zone(0, e, s.ExpectedColor);
                Zone(e, hi, s.HighestColor);
                Zone(hi, max, s.MaximumColor);
            }

            // grey scale ring with ticks and labels
            double scOut = R * 0.78, scIn = R * 0.70;
            dc.DrawGeometry(B(s.ScaleColor), null, Arc(c, scOut, scIn, a0, aEnd));

            double major = NiceStep(max), minor = major / 5;
            var tickPen = new Pen(B(s.ForegroundColor), 1);
            for (double v = 0; v <= max + 0.0001; v += minor)
            {
                bool isMajor = Math.Abs(v / major - Math.Round(v / major)) < 0.0001;
                double ang = vm.ValueToAngle(v);
                dc.DrawLine(tickPen, Pt(c, scIn, ang), Pt(c, isMajor ? scIn - R * 0.07 : scIn - R * 0.035, ang));
                if (isMajor)
                    Centered(dc, Text(v.ToString("0"), R * 0.075, s.ForegroundColor), Pt(c, scIn - R * 0.15, ang));
            }

            // bookmarks
            void Mark(bool show, double value, string label, Color col)
            {
                if (!show) return;
                double ang = vm.ValueToAngle(value);
                var tip = Pt(c, scIn, ang);
                var p1 = Pt(c, scOut + R * 0.01, ang - 3);
                var p2 = Pt(c, scOut + R * 0.01, ang + 3);
                var g = new StreamGeometry();
                using (var ctx = g.Open())
                {
                    ctx.BeginFigure(tip, true, true);
                    ctx.LineTo(p1, true, false);
                    ctx.LineTo(p2, true, false);
                }
                dc.DrawGeometry(B(col), new Pen(B(Colors.Black), 0.5), g);
                Centered(dc, Text(label, R * 0.065, col, FontWeights.Bold), Pt(c, scIn - R * 0.26, ang));
            }
            Mark(vm.ShowAvgMark, vm.AvgValue, "AVG", Colors.Cyan);
            Mark(vm.ShowAvgPlusMark, vm.AvgPlusValue, "AVG+", Colors.Orange);
            Mark(vm.ShowAvgMinusMark, vm.AvgMinusValue, "AVG-", Colors.Gold);
            Mark(vm.ShowMaxMark, vm.MaxValue, "MAX", Colors.Magenta);
            Mark(vm.ShowFeedsMark, vm.FeedsValue, "Feeds", Colors.DeepSkyBlue);
            Mark(vm.ShowOeeMark, vm.OeeValue, "OEE", Colors.Violet);
            Mark(vm.ShowCrewMark, vm.CrewIdValue, "Crew", Colors.White);

            // needle
            double needle = vm.ValueToAngle(vm.PointerValue);
            dc.DrawLine(new Pen(B(s.ForegroundColor), Math.Max(2, R * 0.025)) { EndLineCap = PenLineCap.Triangle },
                c, Pt(c, scOut, needle));
            dc.DrawEllipse(B(s.ForegroundColor), null, c, R * 0.05, R * 0.05);

            // digital labels
            if (s.ShowSpeedLabel)
            {
                Centered(dc, Text(vm.SpeedText, R * 0.26, vm.IsDown ? s.FlashingColor : vm.SpeedColor, FontWeights.SemiBold),
                    new Point(c.X, c.Y + R * 0.25));
                if (!vm.IsDown)
                    Centered(dc, Text(vm.UnitText, R * 0.08, s.ForegroundColor), new Point(c.X, c.Y + R * 0.42));
                else
                    Centered(dc, Text(vm.DowntimeText, R * 0.09, s.FlashingColor, FontWeights.Bold), new Point(c.X, c.Y + R * 0.42));
            }
            if (s.ShowFeedsLabel)
                Centered(dc, Text(vm.FeedsText, R * 0.09, s.ForegroundColor), new Point(c.X - R * 0.38, c.Y + R * 0.3));
            if (s.ShowAvgShiftLabel)
                Centered(dc, Text(vm.AvgShiftText, R * 0.09, s.ForegroundColor), new Point(c.X + R * 0.38, c.Y + R * 0.3));

            // text labels in the gap at the bottom
            double ty = c.Y + R * 0.62;
            void Line(bool show, string t)
            {
                if (!show || string.IsNullOrEmpty(t)) return;
                Centered(dc, Text(t, R * 0.075, s.ForegroundColor), new Point(c.X, ty));
                ty += R * 0.1;
            }
            Line(s.ShowMachineId, d.MachineId);
            Line(s.ShowDescription, d.Description);
            Line(s.ShowJobHandle, d.JobHandle);
            Line(s.ShowCustomText, vm.BottomText);
        }

        private static double NiceStep(double max)
        {
            double raw = max / 10;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double n = raw / mag;
            double nice = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
            return nice * mag;
        }
    }
}
