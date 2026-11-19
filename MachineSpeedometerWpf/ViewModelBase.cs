using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Speedometer
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            RaisePropertyChanged(name);
            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
