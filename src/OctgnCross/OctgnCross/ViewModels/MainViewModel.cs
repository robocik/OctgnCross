using CommunityToolkit.Mvvm.ComponentModel;
using Octgn.UI;

namespace Octgn.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
}