using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    /// <summary>
    /// Speedometer control. Set <see cref="ViewModel"/> (or the DataContext) with a
    /// <see cref="MachineSpeedometerViewModel"/>. A click opens the settings dialog.
    /// </summary>
    public partial class MachineSpeedometerView : UserControl
    {
        public MachineSpeedometerView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Renderer.ViewModel = e.NewValue as MachineSpeedometerViewModel;
            MouseLeftButtonUp += HandleClick;
        }

        public MachineSpeedometerViewModel ViewModel
        {
            get => DataContext as MachineSpeedometerViewModel;
            set => DataContext = value;
        }

        private void HandleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null) return;

            var dlg = new SpeedometerSettingsDialog(vm) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                vm.ApplySettings(dlg.EditedSettings);
        }
    }
}
