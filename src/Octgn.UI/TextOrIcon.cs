using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace OctgnCross.UI;

public class TextOrIcon : ContentControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TextOrIcon, string>(nameof(Text));

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<TextOrIcon, string>(nameof(Icon));

    private readonly Image _img = new Image();
    private readonly TextBlock _tb = new TextBlock();

    public TextOrIcon()
    {
        _img.VerticalAlignment = _tb.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        Focusable = false;
        Content = _tb;

        this.GetObservable(IconProperty).Subscribe(UpdateContent);
        this.GetObservable(TextProperty).Subscribe(UpdateText);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    private void UpdateContent(string icon)
    {
        if (!string.IsNullOrEmpty(icon))
        {
            var bitmap = new Bitmap(icon);
            _img.Source = bitmap;
            Content = _img;
        }
        else
        {
            Content = _tb;
        }
    }

    private void UpdateText(string text)
    {
        // _img.ToolTip = _tb.Text = text;
    }
}