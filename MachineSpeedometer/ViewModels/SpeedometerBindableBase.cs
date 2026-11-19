using System.Runtime.CompilerServices;
using DevExpress.Mvvm;

namespace Witron.Cpms.Shell.Manufacturing.Wpf.Content.ConverterLineUp.ViewModels
{
    public abstract class SpeedometerBindableBase : BindableBase
    {
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
