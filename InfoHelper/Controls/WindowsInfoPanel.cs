using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using WindowState = InfoHelper.ViewModel.DataEntities.WindowState;

namespace InfoHelper.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:InfoHelper.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:InfoHelper.Controls;assembly=InfoHelper.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:WindowsInfoPanel/>
    ///
    /// </summary>
    public class WindowsInfoPanel : BaseDataControl
    {
        private object _okFrontWindowBrush;
        private object _okBackWindowBrush;
        private object _wrongCaptionFrontWindowBrush;
        private object _wrongCaptionBackWindowBrush;
        private object _errorFrontWindowBrush;
        private object _errorBackWindowBrush;

        private object _windowBorderPen;
        private object _heroActingImage;
        private object _heroActingImageSizeRatio;

        static WindowsInfoPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowsInfoPanel), new FrameworkPropertyMetadata(typeof(WindowsInfoPanel)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if(Data == null)
                return;

            VisualEdgeMode = EdgeMode.Aliased;

            _okFrontWindowBrush ??= Application.Current.TryFindResource("OkFrontWindowBackgroundBrush");
            _okBackWindowBrush ??= Application.Current.TryFindResource("OkBackWindowBackgroundBrush");
            _wrongCaptionFrontWindowBrush ??= Application.Current.TryFindResource("WrongCaptionFrontWindowBackgroundBrush");
            _wrongCaptionBackWindowBrush ??= Application.Current.TryFindResource("WrongCaptionBackWindowBackgroundBrush");
            _errorFrontWindowBrush ??= Application.Current.TryFindResource("ErrorFrontWindowBackgroundBrush");
            _errorBackWindowBrush ??= Application.Current.TryFindResource("ErrorBackWindowBackgroundBrush");

            _windowBorderPen ??= Application.Current.TryFindResource("WindowBorderPen");
            _heroActingImage ??= Application.Current.TryFindResource("HeroActingImage");
            _heroActingImageSizeRatio ??= Application.Current.TryFindResource("HeroActingImageSizeRatio");

            object GetBackgroundBrush(WindowState winState)
            {
                return winState switch
                {
                    WindowState.OkFront => _okFrontWindowBrush,
                    WindowState.OkBack => _okBackWindowBrush,
                    WindowState.WrongCaptionFront => _wrongCaptionFrontWindowBrush,
                    WindowState.WrongCaptionBack => _wrongCaptionBackWindowBrush,
                    WindowState.ErrorFront => _errorFrontWindowBrush,
                    WindowState.ErrorBack => _errorBackWindowBrush,
                    _ => null
                };
            }

            WindowInfo[] data = (WindowInfo[])Data;

            Rect[] rectsToRender = new Rect[data.Length];

            Pen pen = null;

            object penResx = _windowBorderPen;

            if (penResx != null)
                pen = (Pen)penResx;

            for (int i = 0; i < data.Length; i++)
            {
                object brushResx = GetBackgroundBrush(data[i].WindowState);

                SolidColorBrush brush = null;

                if (brushResx != null)
                    brush = (SolidColorBrush)brushResx;

                double xRatio = RenderSize.Width / Shared.ClientWorkSpaceWidth;
                double yRatio = RenderSize.Height / Shared.ClientWorkSpaceHeight;

                Rect scaledRect = new Rect((int)(data[i].Rectangle.X * xRatio), (int)(data[i].Rectangle.Y * yRatio), (int)(data[i].Rectangle.Width * xRatio), (int)(data[i].Rectangle.Height * yRatio));

                rectsToRender[i] = scaledRect;

                drawingContext.DrawRectangle(brush, pen, scaledRect);

                if(!data[i].IsHeroActing)
                    continue;

                object imageResx = _heroActingImage;
                object imageSizeResx = _heroActingImageSizeRatio;

                BitmapImage image = null;

                if (imageResx != null)
                    image = (BitmapImage)imageResx;

                int padding = 5;

                if (imageSizeResx != null)
                {
                    double imageSize = (double)imageSizeResx * scaledRect.Width;

                    Rect imageRect = new Rect(scaledRect.X + scaledRect.Width - padding - imageSize, scaledRect.Y + padding, imageSize, imageSize);

                    drawingContext.DrawImage(image, imageRect);
                }
            }

            foreach (Rect rect in rectsToRender)
                drawingContext.DrawRectangle(null, pen, rect);
        }
    }
}
