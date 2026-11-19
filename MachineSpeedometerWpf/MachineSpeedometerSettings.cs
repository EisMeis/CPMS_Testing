using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingAttribute : Attribute
    {
        public SettingAttribute(string category, string displayName)
        {
            Category = category;
            DisplayName = displayName;
        }

        public string Category { get; }
        public string DisplayName { get; }
    }

    /// <summary>All user-changeable settings (the former [DashboardSettingsView] properties).</summary>
    public class MachineSpeedometerSettings : ViewModelBase
    {
        private const string Appearance = "Darstellung";
        private const string Marks = "Markierungen";
        private const string Texts = "Texte";
        private const string Colors_ = "Farben";
        private const string Data = "Daten";

        private bool showAvg = true, showAvgPlus = true, showAvgMinus = true, showMax = true,
            showFeeds = true, showOee = true, showCrewId = true;
        private bool showSpeedLabel = true, showFeedsLabel = true, showAvgShiftLabel = true;
        private bool showMachineId = true, showDescription = true, showJobHandle = true, showCustomText = true;
        private bool transparentMode;
        private PointerSource pointerSource = PointerSource.CurrentSpeed;
        private ScaleRangeType scaleRangeType = ScaleRangeType.ScheduledTimesOfJob;
        private int refreshIntervalSeconds = 5;
        private Color expectedColor = Color.FromRgb(220, 40, 40);
        private Color highestColor = Color.FromRgb(0, 120, 40);
        private Color maximumColor = Color.FromRgb(120, 210, 90);
        private Color flashingColor = Color.FromRgb(255, 60, 60);
        private Color borderColor = Color.FromRgb(90, 90, 90);
        private Color scaleColor = Color.FromRgb(70, 70, 70);
        private Color foregroundColor = Colors.White;
        private Color backgroundColor = Color.FromRgb(30, 30, 30);

        [Setting(Appearance, "Transparenter Modus")] public bool TransparentMode { get => transparentMode; set => SetField(ref transparentMode, value); }
        [Setting(Appearance, "Zeiger zeigt")] public PointerSource PointerSource { get => pointerSource; set => SetField(ref pointerSource, value); }
        [Setting(Appearance, "Geschwindigkeit anzeigen")] public bool ShowSpeedLabel { get => showSpeedLabel; set => SetField(ref showSpeedLabel, value); }
        [Setting(Appearance, "Feeds/h anzeigen")] public bool ShowFeedsLabel { get => showFeedsLabel; set => SetField(ref showFeedsLabel, value); }
        [Setting(Appearance, "Ø Schichtgeschwindigkeit anzeigen")] public bool ShowAvgShiftLabel { get => showAvgShiftLabel; set => SetField(ref showAvgShiftLabel, value); }

        [Setting(Marks, "AVG anzeigen")] public bool ShowAvg { get => showAvg; set => SetField(ref showAvg, value); }
        [Setting(Marks, "AVG+ anzeigen")] public bool ShowAvgPlus { get => showAvgPlus; set => SetField(ref showAvgPlus, value); }
        [Setting(Marks, "AVG- anzeigen")] public bool ShowAvgMinus { get => showAvgMinus; set => SetField(ref showAvgMinus, value); }
        [Setting(Marks, "MAX anzeigen")] public bool ShowMax { get => showMax; set => SetField(ref showMax, value); }
        [Setting(Marks, "Feeds anzeigen")] public bool ShowFeeds { get => showFeeds; set => SetField(ref showFeeds, value); }
        [Setting(Marks, "OEE anzeigen")] public bool ShowOee { get => showOee; set => SetField(ref showOee, value); }
        [Setting(Marks, "Crew-ID anzeigen")] public bool ShowCrewId { get => showCrewId; set => SetField(ref showCrewId, value); }

        [Setting(Texts, "Maschinen-ID anzeigen")] public bool ShowMachineId { get => showMachineId; set => SetField(ref showMachineId, value); }
        [Setting(Texts, "Beschreibung anzeigen")] public bool ShowDescription { get => showDescription; set => SetField(ref showDescription, value); }
        [Setting(Texts, "Job-Handle anzeigen")] public bool ShowJobHandle { get => showJobHandle; set => SetField(ref showJobHandle, value); }
        [Setting(Texts, "Benutzerdefinierten Text anzeigen")] public bool ShowCustomText { get => showCustomText; set => SetField(ref showCustomText, value); }

        [Setting(Colors_, "Farbe erwartet (rot)")] public Color ExpectedColor { get => expectedColor; set => SetField(ref expectedColor, value); }
        [Setting(Colors_, "Farbe höchste Priorität (dunkelgrün)")] public Color HighestColor { get => highestColor; set => SetField(ref highestColor, value); }
        [Setting(Colors_, "Farbe Maximum (hellgrün)")] public Color MaximumColor { get => maximumColor; set => SetField(ref maximumColor, value); }
        [Setting(Colors_, "Farbe Stillstand (blinkend)")] public Color FlashingColor { get => flashingColor; set => SetField(ref flashingColor, value); }
        [Setting(Colors_, "Rahmenfarbe")] public Color BorderColor { get => borderColor; set => SetField(ref borderColor, value); }
        [Setting(Colors_, "Skalenfarbe")] public Color ScaleColor { get => scaleColor; set => SetField(ref scaleColor, value); }
        [Setting(Colors_, "Schriftfarbe")] public Color ForegroundColor { get => foregroundColor; set => SetField(ref foregroundColor, value); }
        [Setting(Colors_, "Hintergrundfarbe")] public Color BackgroundColor { get => backgroundColor; set => SetField(ref backgroundColor, value); }

        [Setting(Data, "Skalenbereich")] public ScaleRangeType ScaleRangeType { get => scaleRangeType; set => SetField(ref scaleRangeType, value); }
        [Setting(Data, "Aktualisierung (Sekunden)")] public int RefreshIntervalSeconds { get => refreshIntervalSeconds; set => SetField(ref refreshIntervalSeconds, Math.Max(1, value)); }

        private static PropertyInfo[] Props => typeof(MachineSpeedometerSettings).GetProperties()
            .Where(p => p.GetCustomAttribute<SettingAttribute>() != null).ToArray();

        public MachineSpeedometerSettings Clone()
        {
            var c = new MachineSpeedometerSettings();
            c.CopyFrom(this);
            return c;
        }

        public void CopyFrom(MachineSpeedometerSettings other)
        {
            foreach (var p in Props)
                p.SetValue(this, p.GetValue(other));
        }

        public static string DefaultPath(string key) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witron", "Speedometer", key + ".xml");

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var el = new XElement("MachineSpeedometer");
            foreach (var p in Props)
                el.SetAttributeValue(p.Name, TypeDescriptor.GetConverter(p.PropertyType).ConvertToInvariantString(p.GetValue(this)));
            new XDocument(el).Save(path);
        }

        /// <summary>Loads values from the xml; unknown or invalid attributes are ignored (defaults stay).</summary>
        public void Load(string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                var el = XDocument.Load(path).Root;
                foreach (var p in Props)
                {
                    var a = el?.Attribute(p.Name);
                    if (a == null) continue;
                    try { p.SetValue(this, TypeDescriptor.GetConverter(p.PropertyType).ConvertFromInvariantString(a.Value)); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }
}
