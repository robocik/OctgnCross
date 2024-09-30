using System.ComponentModel;
using Avalonia.Controls;
using Octgn.Core.Annotations;

namespace OctgnCross.UI;

public abstract class WindowBase:Window,INotifyPropertyChanged
{
    #region PropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged( string propertyName ) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion PropertyChanged
}