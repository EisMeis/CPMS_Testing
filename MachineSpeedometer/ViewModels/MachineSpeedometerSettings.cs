using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SpeedometerSettingAttribute : Attribute
    {
        public SpeedometerSettingAttribute(string category, string displayName)
        {
            this.Category = category;
            this.DisplayName = displayName;
        }

        public string Category { get; }

        public string DisplayName { get; }
    }

    /// <summary>All user-changeable settings (the former [DashboardSettingsView] properties).</summary>
    public class MachineSpeedometerSettings : SpeedometerBindableBase
    {
        private const string Appearance = "Darstellung";
        private const string Marks = "Markierungen";
        private const string Texts = "Texte";
        private const string ColorsCategory = "Farben";
        private const string DataCategory = "Daten";

        private bool transparentMode;
        private PointerSource pointerSource = PointerSource.CurrentSpeed;
        private bool showSpeedLabel = true;
        private bool showFeedsLabel = true;
        private bool showAvgShiftLabel = true;

        private bool showAvg = true;
        private bool showAvgPlus = true;
        private bool showAvgMinus = true;
        private bool showMax = true;
        private bool showFeeds = true;
        private bool showOee = true;
        private bool showCrewId = true;

        private bool showMachineId = true;
        private bool showDescription = true;
        private bool showJobHandle = true;
        private bool showCustomText = true;

        private Color expectedColor = Color.FromRgb(220, 40, 40);
        private Color highestColor = Color.FromRgb(0, 120, 40);
        private Color maximumColor = Color.FromRgb(120, 210, 90);
        private Color flashingColor = Color.FromRgb(255, 60, 60);
        private Color foregroundColor = Colors.White;
        private Color backgroundColor = Color.FromRgb(30, 30, 30);

        private ScaleRangeType scaleRangeType = ScaleRangeType.ScheduledTimesOfJob;
        private int refreshIntervalSeconds = 5;

        [SpeedometerSetting(Appearance, "Transparenter Modus")]
        public bool TransparentMode { get => this.transparentMode; set => this.Set(ref this.transparentMode, value); }

        [SpeedometerSetting(Appearance, "Zeiger zeigt")]
        public PointerSource PointerSource { get => this.pointerSource; set => this.Set(ref this.pointerSource, value); }

        [SpeedometerSetting(Appearance, "Geschwindigkeit anzeigen")]
        public bool ShowSpeedLabel { get => this.showSpeedLabel; set => this.Set(ref this.showSpeedLabel, value); }

        [SpeedometerSetting(Appearance, "Feeds/h anzeigen")]
        public bool ShowFeedsLabel { get => this.showFeedsLabel; set => this.Set(ref this.showFeedsLabel, value); }

        [SpeedometerSetting(Appearance, "Ø Schichtgeschwindigkeit anzeigen")]
        public bool ShowAvgShiftLabel { get => this.showAvgShiftLabel; set => this.Set(ref this.showAvgShiftLabel, value); }

        [SpeedometerSetting(Marks, "AVG anzeigen")]
        public bool ShowAvg { get => this.showAvg; set => this.Set(ref this.showAvg, value); }

        [SpeedometerSetting(Marks, "AVG+ anzeigen")]
        public bool ShowAvgPlus { get => this.showAvgPlus; set => this.Set(ref this.showAvgPlus, value); }

        [SpeedometerSetting(Marks, "AVG- anzeigen")]
        public bool ShowAvgMinus { get => this.showAvgMinus; set => this.Set(ref this.showAvgMinus, value); }

        [SpeedometerSetting(Marks, "MAX anzeigen")]
        public bool ShowMax { get => this.showMax; set => this.Set(ref this.showMax, value); }

        [SpeedometerSetting(Marks, "Feeds anzeigen")]
        public bool ShowFeeds { get => this.showFeeds; set => this.Set(ref this.showFeeds, value); }

        [SpeedometerSetting(Marks, "OEE anzeigen")]
        public bool ShowOee { get => this.showOee; set => this.Set(ref this.showOee, value); }

        [SpeedometerSetting(Marks, "Crew-ID anzeigen")]
        public bool ShowCrewId { get => this.showCrewId; set => this.Set(ref this.showCrewId, value); }

        [SpeedometerSetting(Texts, "Maschinen-ID anzeigen")]
        public bool ShowMachineId { get => this.showMachineId; set => this.Set(ref this.showMachineId, value); }

        [SpeedometerSetting(Texts, "Beschreibung anzeigen")]
        public bool ShowDescription { get => this.showDescription; set => this.Set(ref this.showDescription, value); }

        [SpeedometerSetting(Texts, "Job-Handle anzeigen")]
        public bool ShowJobHandle { get => this.showJobHandle; set => this.Set(ref this.showJobHandle, value); }

        [SpeedometerSetting(Texts, "Benutzerdefinierten Text anzeigen")]
        public bool ShowCustomText { get => this.showCustomText; set => this.Set(ref this.showCustomText, value); }

        [SpeedometerSetting(ColorsCategory, "Farbe erwartet (rot)")]
        public Color ExpectedColor { get => this.expectedColor; set => this.Set(ref this.expectedColor, value); }

        [SpeedometerSetting(ColorsCategory, "Farbe höchste Priorität (dunkelgrün)")]
        public Color HighestColor { get => this.highestColor; set => this.Set(ref this.highestColor, value); }

        [SpeedometerSetting(ColorsCategory, "Farbe Maximum (hellgrün)")]
        public Color MaximumColor { get => this.maximumColor; set => this.Set(ref this.maximumColor, value); }

        [SpeedometerSetting(ColorsCategory, "Farbe Stillstand (blinkend)")]
        public Color FlashingColor { get => this.flashingColor; set => this.Set(ref this.flashingColor, value); }

        [SpeedometerSetting(ColorsCategory, "Schriftfarbe")]
        public Color ForegroundColor { get => this.foregroundColor; set => this.Set(ref this.foregroundColor, value); }

        [SpeedometerSetting(ColorsCategory, "Hintergrundfarbe")]
        public Color BackgroundColor { get => this.backgroundColor; set => this.Set(ref this.backgroundColor, value); }

        [SpeedometerSetting(DataCategory, "Skalenbereich")]
        public ScaleRangeType ScaleRangeType { get => this.scaleRangeType; set => this.Set(ref this.scaleRangeType, value); }

        [SpeedometerSetting(DataCategory, "Aktualisierung (Sekunden)")]
        public int RefreshIntervalSeconds { get => this.refreshIntervalSeconds; set => this.Set(ref this.refreshIntervalSeconds, Math.Max(1, value)); }

        public static PropertyInfo[] SettingProperties { get; } = typeof(MachineSpeedometerSettings)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<SpeedometerSettingAttribute>() != null)
            .ToArray();

        public static string DefaultPath(string key) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witron", "Speedometer", key + ".xml");

        public MachineSpeedometerSettings Clone()
        {
            var copy = new MachineSpeedometerSettings();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(MachineSpeedometerSettings other)
        {
            foreach (var p in SettingProperties)
                p.SetValue(this, p.GetValue(other));
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var element = new XElement("MachineSpeedometer");
            foreach (var p in SettingProperties)
                element.SetAttributeValue(p.Name, TypeDescriptor.GetConverter(p.PropertyType).ConvertToInvariantString(p.GetValue(this)));
            new XDocument(element).Save(path);
        }

        /// <summary>Loads values from the xml; missing or invalid attributes keep their defaults.</summary>
        public void Load(string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                var element = XDocument.Load(path).Root;
                foreach (var p in SettingProperties)
                {
                    var attribute = element?.Attribute(p.Name);
                    if (attribute == null)
                        continue;

                    try
                    {
                        p.SetValue(this, TypeDescriptor.GetConverter(p.PropertyType).ConvertFromInvariantString(attribute.Value));
                    }
                    catch (Exception)
                    {
                        // invalid value: keep default
                    }
                }
            }
            catch (Exception)
            {
                // unreadable file: keep defaults
            }
        }
    }
}
