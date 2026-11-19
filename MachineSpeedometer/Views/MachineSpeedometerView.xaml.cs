using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.Views
{
    /// <summary>
    /// Speedometer control. Set the DataContext to a <see cref="MachineSpeedometerViewModel"/>.
    /// A click on the gauge opens the settings dialog.
    /// </summary>
    public partial class MachineSpeedometerView : UserControl
    {
        public MachineSpeedometerView()
        {
            this.InitializeComponent();
            this.PreviewMouseLeftButtonUp += this.HandleClick;
        }

        private void HandleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(this.DataContext is MachineSpeedometerViewModel viewModel))
                return;

            var dialog = new SpeedometerSettingsDialog(viewModel) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
                viewModel.ApplySettings(dialog.EditedSettings);
        }
    }
}
