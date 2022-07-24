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
        static WindowsInfoPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowsInfoPanel), new FrameworkPropertyMetadata(typeof(WindowsInfoPanel)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if(Data == null)
                return;

            object GetBackgroundBrush(WindowState winState)
            {
                return winState switch
                {
                    WindowState.OkFront => Application.Current.TryFindResource("OkFrontWindowBackgroundBrush"),
                    WindowState.OkBack => Application.Current.TryFindResource("OkBackWindowBackgroundBrush"),
                    WindowState.WrongCaptionFront => Application.Current.TryFindResource("WrongCaptionFrontWindowBackgroundBrush"),
                    WindowState.WrongCaptionBack => Application.Current.TryFindResource("WrongCaptionBackWindowBackgroundBrush"),
                    WindowState.ErrorFront => Application.Current.TryFindResource("ErrorFrontWindowBackgroundBrush"),
                    WindowState.ErrorBack => Application.Current.TryFindResource("ErrorBackWindowBackgroundBrush"),
                    _ => null
                };
            }

            WindowInfo[] data = (WindowInfo[])Data;

            for (int i = 0; i < data.Length; i++)
            {
                object brushResx = GetBackgroundBrush(data[i].WindowState);
                object penResx = Application.Current.TryFindResource("WindowBorderPen");

                SolidColorBrush brush = null;
                Pen pen = null;

                if (brushResx != null)
                    brush = (SolidColorBrush)brushResx;

                if (penResx != null)
                    pen = (Pen)penResx;

                object clientScreenWidthResx = Application.Current.Resources["ClientScreenWidth"], clientScreenHeight = Application.Current.Resources["ClientScreenHeight"];

                double xRatio = 0, yRatio = 0;

                if (clientScreenWidthResx != null && clientScreenHeight != null)
                {
                    xRatio = RenderSize.Width / (int)clientScreenWidthResx;
                    yRatio = RenderSize.Height / (int)clientScreenHeight;
                }

                Rect scaledRect = new Rect(data[i].Rectangle.X * xRatio, data[i].Rectangle.Y * yRatio, data[i].Rectangle.Width * xRatio, data[i].Rectangle.Height * yRatio);

                drawingContext.DrawRectangle(brush, pen, scaledRect);

                if(!data[i].IsHeroActing)
                    continue;

                object imageResx = Application.Current.TryFindResource("HeroActingImage");
                object imageSizeResx = Application.Current.TryFindResource("HeroActingImageSizeRatio");

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
        }
    }
}
