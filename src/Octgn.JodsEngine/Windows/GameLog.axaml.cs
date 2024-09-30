using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octgn.Play.Gui;

namespace Octgn.JodsEngine.Windows;

public partial class GameLog : UserControl
{
    private bool realClose = false;
    public GameLog()
    {
        InitializeComponent();
        // this.Closing += OnClosing;
    }

    private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
    {
        if (!realClose)
        {
            this.IsVisible = false;
            cancelEventArgs.Cancel = true;
        }
    }

    public void RealClose()
    {
        realClose = true;
        // Close();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // ChatControl.Save();
    }
}