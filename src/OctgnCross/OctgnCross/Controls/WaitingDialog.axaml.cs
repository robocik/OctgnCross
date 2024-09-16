using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;

namespace OctgnCross.Controls;

public enum DialogResult
{
    //
    // Summary:
    //     Nothing is returned from the dialog box. This means that the modal dialog continues
    //     running.
    None,
    //
    // Summary:
    //     The dialog box return value is OK (usually sent from a button labeled OK).
    OK,
    //
    // Summary:
    //     The dialog box return value is Cancel (usually sent from a button labeled Cancel).
    Cancel,
    //
    // Summary:
    //     The dialog box return value is Abort (usually sent from a button labeled Abort).
    Abort,
    //
    // Summary:
    //     The dialog box return value is Retry (usually sent from a button labeled Retry).
    Retry,
    //
    // Summary:
    //     The dialog box return value is Ignore (usually sent from a button labeled Ignore).
    Ignore,
    //
    // Summary:
    //     The dialog box return value is Yes (usually sent from a button labeled Yes).
    Yes,
    //
    // Summary:
    //     The dialog box return value is No (usually sent from a button labeled No).
    No
}

public partial class WaitingDialog : UserControl, INotifyPropertyChanged ,IDisposable
{
    public string Message
    {
        get
        {
            return this.message;
        }
        set
        {
            if (value == this.message) return;
            this.message = value;
            OnPropertyChanged("Message");
        }
    }

    public string Title
    {
        get
        {
            return this.title;
        }
        set
        {
            if (value == this.title) return;
            this.title = value;
            OnPropertyChanged("Title");
        }
    }

    public event Func<object, DialogResult,Task> OnClose;
    protected virtual async  Task FireOnClose(object sender, DialogResult result)
    {
        if (OnClose != null)
            await this.OnClose(sender, result);
    }

    private Decorator Placeholder;

    private string message;

    private string title;

    public WaitingDialog()
    {
        InitializeComponent();
    }

    #region Dialog
    public async Task Show(Decorator placeholder, Func<Task> action, string argtitle, string argmessage)
    {
        Placeholder = placeholder;
        placeholder.Child = this;
        Title = argtitle;
        Message = argmessage;
        
        // var task = new Task(action);
        // task.ContinueWith((x) => this.Close());
        // task.Start();
        
        try
        {
            // Wywołanie asynchronicznej funkcji i czekanie na jej zakończenie
            await action();
        }
        finally
        {
            // Zamknięcie po zakończeniu działania taska
            this.Close();
        }
    }

    public void UpdateMessage(string message)
    {
        Dispatcher.UIThread.Invoke(() =>
            { this.Message = message; });
    }

    public void Close()
    {
        Close(DialogResult.Abort);
    }

    private void Close(DialogResult result)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            ProgressBar.IsIndeterminate = false;
            this.Placeholder.Child = null;
            await this.FireOnClose(this, result);
        });
    }
    #endregion

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Implementation of IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {

        if (OnClose != null)
        {
            foreach (var d in OnClose.GetInvocationList())
            {
                OnClose -= (Func<object, DialogResult,Task>)d;
            }
        }
        if (PropertyChanged != null)
        {
            foreach (var d in PropertyChanged.GetInvocationList())
            {
                PropertyChanged -= (PropertyChangedEventHandler)d;
            }
        }
    }

    #endregion
}