using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiteDB.Studio.Wpf.Util;

/// <summary>
/// An <see cref="Image"/> that automatically switches to a gray (desaturated) version of its source
/// when the control is disabled (<see cref="UIElement.IsEnabled"/> = <c>false</c>).
/// </summary>
public class GrayableImage : Image
{
    private ImageSource? _originalSource;
    private ImageSource? _graySource;
    private bool _isUpdating;

    /// <summary>Initializes a new instance of <see cref="GrayableImage"/> and subscribes to state-change events.</summary>
    public GrayableImage()
    {
        IsEnabledChanged += GrayableImage_IsEnabledChanged;

        var desc = System.ComponentModel.DependencyPropertyDescriptor
            .FromProperty(SourceProperty, typeof(Image));

        desc?.AddValueChanged(this, SourcePropertyChanged);
    }

    private void SourcePropertyChanged(object? sender, EventArgs e)
    {
        if (_isUpdating) {
            return;
        }

        _originalSource = Source;
        _graySource = CreateGraySource(_originalSource);

        _isUpdating = true;
        try
        {
            ImageSource? newSource = IsEnabled ? _originalSource : _graySource;
            if (!ReferenceEquals(Source, newSource))
            {
                Source = newSource;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void GrayableImage_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_isUpdating) {
            return;
        }

        if (_originalSource == null)
        {
            _originalSource = Source;
            _graySource = CreateGraySource(_originalSource);
        }

        _isUpdating = true;
        try
        {
            ImageSource? newSource = IsEnabled ? _originalSource : _graySource;
            if (!ReferenceEquals(Source, newSource))
            {
                Source = newSource;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private static ImageSource? CreateGraySource(ImageSource? src)
    {
        if (src is not BitmapSource bmp) { return src; }

        try
        {
            var conv = new FormatConvertedBitmap(bmp, PixelFormats.Gray8, null, 0);
            conv.Freeze();
            return conv;
        }
        catch
        {
            return src;
        }
    }
}
