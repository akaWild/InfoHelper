using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StatUtility;

namespace InfoHelper.Controls
{
    public class HudBase : UserControl
    {
        protected readonly ToolTip HudToolTip;

        public HudBase()
        {
            HudToolTip = new ToolTip();

            ToolTip = HudToolTip;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            HudToolTip.IsOpen = false;

            if (e.OriginalSource is not FrameworkElement fe)
                return;

            DataCell dc;

            if (fe.TemplatedParent is DataCellControl dataCellControl)
                dc = dataCellControl.Data;
            else if (fe.TemplatedParent is GeneralDataCellControl generalDataCellControl)
                dc = generalDataCellControl.Data;
            else
                return;

            if (dc is ValueCell or EvCell)
                return;

            HudToolTip.Content = $"{dc.Description}: {dc.Value}/{dc.Sample}";

            if (!float.IsNaN(dc.DefaultValue))
                HudToolTip.Content += $" Dflt value: {Math.Round(dc.DefaultValue, 1).ToString(CultureInfo.InvariantCulture)}%";

            HudToolTip.IsOpen = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            HudToolTip.IsOpen = false;
        }
    }
}
