namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MachineSpeedometerViewModel(new SimulatedDataSource(), "demo");
            Speedometer.ViewModel = vm;
            Closed += (s, e) => vm.Dispose();
        }
    }
}
