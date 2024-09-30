using System;
using Octgn.UI;
using Octgn.ViewModels;

namespace Octgn.Tabs.Login;

public class NewsItemViewModel:ViewModelBase
{
    public DateTimeOffset Time {
        get { return _time; }
        set { base.SetProperty(ref _time, value); }
    }
    private DateTimeOffset _time;

    public string Message {
        get { return _message; }
        set { base.SetProperty(ref _message, value); }
    }
    private string _message;

    public NewsItemViewModel(DateTimeOffset time, string message) {
        Message = message;
        Time = time;
    }
}