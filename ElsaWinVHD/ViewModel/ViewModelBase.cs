using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElsaWinVHD.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName()] string name = null)
        {
            if (name != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        protected bool SetProperty<T>(ref T backingField, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(backingField, newValue)) return false;

            backingField = newValue;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
