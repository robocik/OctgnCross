using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace Octgn.Controls;

public class Hyperlink : TextBlock
{
    public event EventHandler Click;
    
    public static readonly StyledProperty<Uri> NavigateUriProperty =
        AvaloniaProperty.Register<Hyperlink, Uri>(nameof(NavigateUri));

    public Uri? NavigateUri
    {
        get => GetValue(NavigateUriProperty);
        set => SetValue(NavigateUriProperty, value);
    }

    public Hyperlink()
    {
        this.PointerEntered += OnPointerEnter;
        this.PointerExited += OnPointerLeave;
        this.PointerPressed += OnPointerPressed;
        this.Cursor = new Cursor(StandardCursorType.Hand);
        this.Foreground = Brushes.Blue;
        this.TextDecorations = Avalonia.Media.TextDecorations.Underline;

    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        this.Foreground = Brushes.CornflowerBlue;
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        this.Foreground = Brushes.Blue;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Click?.Invoke(this, EventArgs.Empty);

        // Obsługa otwierania linku w przeglądarce, jeśli NavigateUri jest ustawione
        if (NavigateUri != null)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = NavigateUri.ToString(),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Obsługa wyjątków (np. brak przeglądarki)
            }
        }
    }
}
