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

namespace InfoHelper.Controls
{
    /// <summary>
    /// Interaction logic for HudsPanel.xaml
    /// </summary>
    public partial class HudsPanel : Grid
    {
        public HudsPanel()
        {
            InitializeComponent();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Application.Current.Resources["CellHeight"] = 0.06 * sizeInfo.NewSize.Height;
        }
    }
}
