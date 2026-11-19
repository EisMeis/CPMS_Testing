using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    /// <summary>Settings dialog built from the [Setting] attributes of <see cref="MachineSpeedometerSettings"/>.</summary>
    public sealed class SpeedometerSettingsDialog : Window
    {
        public SpeedometerSettingsDialog(MachineSpeedometerViewModel vm)
        {
            EditedSettings = vm.Settings.Clone();
            Title = "Speedometer – Einstellungen";
            Width = 460;
            Height = 620;
            WindowStartupLocation = Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            var tabs = new TabControl { Margin = new Thickness(8) };
            var groups = typeof(MachineSpeedometerSettings).GetProperties()
                .Select(p => (Prop: p, Attr: p.GetCustomAttribute<SettingAttribute>()))
                .Where(x => x.Attr != null)
                .GroupBy(x => x.Attr.Category);

            foreach (var g in groups)
            {
                var grid = new Grid { Margin = new Thickness(8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                int row = 0;
                foreach (var (prop, attr) in g)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var label = new TextBlock { Text = attr.DisplayName, Margin = new Thickness(0, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                    var editor = CreateEditor(prop);
                    editor.Margin = new Thickness(0, 4, 0, 4);
                    Grid.SetRow(label, row);
                    Grid.SetRow(editor, row);
                    Grid.SetColumn(editor, 1);
                    grid.Children.Add(label);
                    grid.Children.Add(editor);
                    row++;
                }
                tabs.Items.Add(new TabItem { Header = g.Key, Content = new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
            }

            tabs.Items.Add(new TabItem { Header = "Info", Content = CreateInfo(vm.InfoItems) });

            var ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(4), IsDefault = true };
            ok.Click += (s, e) => { DialogResult = true; };
            var cancel = new Button { Content = "Abbrechen", Width = 80, Margin = new Thickness(4), IsCancel = true };
            var reset = new Button { Content = "Standard", Width = 80, Margin = new Thickness(4) };
            reset.Click += (s, e) =>
            {
                EditedSettings.CopyFrom(new MachineSpeedometerSettings());
                // re-read the bindings
                foreach (var tb in FindAll<TextBox>(tabs)) tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                foreach (var cb in FindAll<CheckBox>(tabs)) cb.GetBindingExpression(ToggleButton_IsChecked)?.UpdateTarget();
                foreach (var co in FindAll<ComboBox>(tabs)) co.GetBindingExpression(Selector.SelectedItemProperty)?.UpdateTarget();
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 0, 8, 8) };
            buttons.Children.Add(reset);
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var root = new DockPanel();
            DockPanel.SetDock(buttons, Dock.Bottom);
            root.Children.Add(buttons);
            root.Children.Add(tabs);
            Content = root;
        }

        private static readonly DependencyProperty ToggleButton_IsChecked = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;

        public MachineSpeedometerSettings EditedSettings { get; }

        private FrameworkElement CreateEditor(PropertyInfo prop)
        {
            var binding = new Binding(prop.Name) { Source = EditedSettings, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            var t = prop.PropertyType;

            if (t == typeof(bool))
            {
                var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
                cb.SetBinding(ToggleButton_IsChecked, binding);
                return cb;
            }
            if (t.IsEnum)
            {
                var combo = new ComboBox { ItemsSource = Enum.GetValues(t) };
                combo.SetBinding(Selector.SelectedItemProperty, binding);
                return combo;
            }
            if (t == typeof(Color))
            {
                var tb = new TextBox();
                tb.SetBinding(TextBox.TextProperty, binding);
                var swatch = new Border { Width = 22, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(4, 0, 0, 0) };
                var sb = new Binding(prop.Name) { Source = EditedSettings, Converter = new ColorToBrushConverter() };
                swatch.SetBinding(Border.BackgroundProperty, sb);
                var dock = new DockPanel();
                DockPanel.SetDock(swatch, Dock.Right);
                dock.Children.Add(swatch);
                dock.Children.Add(tb);
                tb.ToolTip = "Farbe als #RRGGBB oder Name (z. B. Red)";
                return dock;
            }
            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, binding);
            return box;
        }

        private static UIElement CreateInfo(IEnumerable<KeyValuePair<string, string>> items)
        {
            var grid = new Grid { Margin = new Thickness(8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int row = 0;
            foreach (var kv in items)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var k = new TextBlock { Text = kv.Key, Margin = new Thickness(0, 3, 8, 3) };
                var v = new TextBox { Text = kv.Value ?? string.Empty, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent };
                Grid.SetRow(k, row);
                Grid.SetRow(v, row);
                Grid.SetColumn(v, 1);
                grid.Children.Add(k);
                grid.Children.Add(v);
                row++;
            }
            return new ScrollViewer { Content = grid };
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var ch = VisualTreeHelper.GetChild(root, i);
                if (ch is T t) yield return t;
                foreach (var x in FindAll<T>(ch)) yield return x;
            }
        }

        private sealed class ColorToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => value is Color c ? new SolidColorBrush(c) : Brushes.Transparent;

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => Binding.DoNothing;
        }
    }
}
