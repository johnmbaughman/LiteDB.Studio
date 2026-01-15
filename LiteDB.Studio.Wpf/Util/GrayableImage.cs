using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiteDB.Studio.Wpf.Util
{
    public class GrayableImage : Image
    {
        private ImageSource? _originalSource;
        private ImageSource? _graySource;
        private bool _isUpdating;

        public GrayableImage()
        {
            this.IsEnabledChanged += GrayableImage_IsEnabledChanged;

            var desc = System.ComponentModel.DependencyPropertyDescriptor
                .FromProperty(Image.SourceProperty, typeof(Image));

            if (desc != null)
            {
                desc.AddValueChanged(this, SourcePropertyChanged);
            }
        }

        private void SourcePropertyChanged(object sender, System.EventArgs e)
        {
            if (_isUpdating) return;

            _originalSource = base.Source;
            _graySource = CreateGraySource(_originalSource);

            _isUpdating = true;
            try
            {
                var newSource = IsEnabled ? _originalSource : _graySource;
                if (!ReferenceEquals(base.Source, newSource))
                {
                    base.Source = newSource;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void GrayableImage_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (_originalSource == null)
            {
                _originalSource = base.Source;
                _graySource = CreateGraySource(_originalSource);
            }

            _isUpdating = true;
            try
            {
                var newSource = IsEnabled ? _originalSource : _graySource;
                if (!ReferenceEquals(base.Source, newSource))
                {
                    base.Source = newSource;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private ImageSource CreateGraySource(ImageSource src)
        {
            if (src is BitmapSource bmp)
            {
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

            return src;
        }
    }
}
